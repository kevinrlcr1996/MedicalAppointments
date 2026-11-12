using MedicalAppointments.Application.Common.Exceptions;
using MedicalAppointments.Application.MedicalStructure.Doctors;
using MedicalAppointments.Application.MedicalStructure.Rooms;
using MedicalAppointments.Application.Scheduling.Blockouts;
using MedicalAppointments.Application.Scheduling.Common;
using MedicalAppointments.Application.Scheduling.DoctorSchedules;
using MedicalAppointments.Application.Scheduling.Holidays;
using MedicalAppointments.Application.Scheduling.ScheduleExceptions;

namespace MedicalAppointments.Application.Scheduling.Availability;

public sealed class AvailabilityService : IAvailabilityService
{
    private readonly IDoctorStore _doctorStore;
    private readonly IRoomStore _roomStore;
    private readonly ISchedulingClinicLookup _clinicLookup;
    private readonly IDoctorScheduleStore _scheduleStore;
    private readonly IScheduleExceptionStore _exceptionStore;
    private readonly IBlockoutStore _blockoutStore;
    private readonly IHolidayStore _holidayStore;
    private readonly IAppointmentOccupancyProvider _occupancyProvider;
    private readonly ITimeZoneConverter _timeZoneConverter;

    public AvailabilityService(
        IDoctorStore doctorStore,
        IRoomStore roomStore,
        ISchedulingClinicLookup clinicLookup,
        IDoctorScheduleStore scheduleStore,
        IScheduleExceptionStore exceptionStore,
        IBlockoutStore blockoutStore,
        IHolidayStore holidayStore,
        IAppointmentOccupancyProvider occupancyProvider,
        ITimeZoneConverter timeZoneConverter)
    {
        _doctorStore = doctorStore;
        _roomStore = roomStore;
        _clinicLookup = clinicLookup;
        _scheduleStore = scheduleStore;
        _exceptionStore = exceptionStore;
        _blockoutStore = blockoutStore;
        _holidayStore = holidayStore;
        _occupancyProvider = occupancyProvider;
        _timeZoneConverter = timeZoneConverter;
    }

    public async Task<AvailabilityDayDto> GetDayAsync(
        AvailabilityQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!await _doctorStore.ExistsAsync(query.DoctorId, cancellationToken))
        {
            throw new NotFoundException("Doctor not found.");
        }

        var clinic = await _clinicLookup.GetContextAsync(query.ClinicId, cancellationToken)
            ?? throw new NotFoundException("Clinic not found.");

        var timeZoneId = string.IsNullOrWhiteSpace(clinic.TimeZoneId) ? "UTC" : clinic.TimeZoneId;

        if (query.RoomId.HasValue)
        {
            var room = await _roomStore.GetTrackedByIdAsync(query.RoomId.Value, cancellationToken);
            if (room is null || room.ClinicId != query.ClinicId)
            {
                throw new NotFoundException("Room not found in the selected clinic.");
            }

            if (!room.IsActive)
            {
                return Empty(query, timeZoneId);
            }
        }

        if (!clinic.IsActive)
        {
            return Empty(query, timeZoneId);
        }

        if (await _holidayStore.ExistsForDateAsync(clinic.OrganizationId, clinic.Id, query.Date, cancellationToken))
        {
            return Empty(query, timeZoneId);
        }

        var schedule = await _scheduleStore.GetActiveForDateAsync(
            query.DoctorId,
            query.ClinicId,
            query.Date,
            cancellationToken);

        if (schedule is null)
        {
            return Empty(query, timeZoneId);
        }

        var scheduleDay = schedule.Days.FirstOrDefault(x => x.DayOfWeek == query.Date.DayOfWeek);

        if (scheduleDay is null)
        {
            return Empty(query, timeZoneId);
        }

        var slotDuration = query.SlotDurationMinutes ?? schedule.SlotDurationMinutes;

        var localSlots = GenerateLocalSlots(
            scheduleDay.StartTime,
            scheduleDay.EndTime,
            slotDuration,
            schedule.BufferMinutes);

        var removals = new List<TimeRange>();

        var exceptions = await _exceptionStore.GetForDateAsync(
            query.DoctorId,
            query.ClinicId,
            query.Date,
            cancellationToken);

        foreach (var exception in exceptions.Where(x => x.IsActive))
        {
            if (exception.IsAllDay)
            {
                return Empty(query, timeZoneId);
            }

            if (exception.StartTime.HasValue &&
                exception.EndTime.HasValue &&
                _timeZoneConverter.TryConvertLocalToUtc(query.Date, exception.StartTime.Value, timeZoneId, out var exStart) &&
                _timeZoneConverter.TryConvertLocalToUtc(query.Date, exception.EndTime.Value, timeZoneId, out var exEnd))
            {
                removals.Add(new TimeRange(exStart, exEnd));
            }
        }

        var dayRange = _timeZoneConverter.GetLocalDayRangeUtc(query.Date, timeZoneId);

        var blockouts = await _blockoutStore.GetDoctorOverlappingAsync(
            query.DoctorId,
            dayRange.StartAt,
            dayRange.EndAt,
            cancellationToken);

        removals.AddRange(
            blockouts
                .Where(x => x.IsActive)
                .Select(x => new TimeRange(x.StartAt, x.EndAt)));

        var occupied = await _occupancyProvider.GetOccupiedRangesAsync(
            query.DoctorId,
            query.ClinicId,
            dayRange.StartAt,
            dayRange.EndAt,
            cancellationToken);

        removals.AddRange(occupied);

        if (query.RoomId.HasValue)
        {
            var roomBlockouts = await _blockoutStore.GetRoomOverlappingAsync(
                query.RoomId.Value,
                dayRange.StartAt,
                dayRange.EndAt,
                cancellationToken);
            removals.AddRange(roomBlockouts.Select(x => new TimeRange(x.StartAt, x.EndAt)));

            var roomOccupied = await _occupancyProvider.GetOccupiedRoomRangesAsync(
                query.RoomId.Value,
                dayRange.StartAt,
                dayRange.EndAt,
                cancellationToken);
            removals.AddRange(roomOccupied);
        }

        var slots = new List<AvailabilitySlotDto>();

        foreach (var localSlot in localSlots)
        {
            if (!_timeZoneConverter.TryConvertLocalToUtc(query.Date, localSlot.Start, timeZoneId, out var startUtc))
            {
                continue;
            }

            if (!_timeZoneConverter.TryConvertLocalToUtc(query.Date, localSlot.End, timeZoneId, out var endUtc))
            {
                continue;
            }

            var slotRange = new TimeRange(startUtc, endUtc);

            if (removals.Any(r => r.IsValid && r.Overlaps(slotRange)))
            {
                continue;
            }

            slots.Add(new AvailabilitySlotDto(
                query.DoctorId,
                query.ClinicId,
                query.Date,
                startUtc,
                endUtc,
                localSlot.Start,
                localSlot.End,
                slotDuration,
                timeZoneId));
        }

        return new AvailabilityDayDto(
            query.DoctorId,
            query.ClinicId,
            query.Date,
            timeZoneId,
            slots.OrderBy(x => x.StartAtUtc).ToList());
    }

    private static IEnumerable<(TimeOnly Start, TimeOnly End)> GenerateLocalSlots(
        TimeOnly start,
        TimeOnly end,
        int slotDurationMinutes,
        int bufferMinutes)
    {
        var cursor = start;

        while (true)
        {
            var slotEnd = cursor.AddMinutes(slotDurationMinutes);

            if (slotEnd > end)
            {
                yield break;
            }

            yield return (cursor, slotEnd);

            var next = slotEnd.AddMinutes(bufferMinutes);

            if (next <= cursor)
            {
                yield break;
            }

            cursor = next;
        }
    }

    private static AvailabilityDayDto Empty(AvailabilityQuery query, string timeZoneId)
    {
        return new AvailabilityDayDto(
            query.DoctorId,
            query.ClinicId,
            query.Date,
            timeZoneId,
            Array.Empty<AvailabilitySlotDto>());
    }
}
