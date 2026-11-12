using MedicalAppointments.Application.Reports;
using MedicalAppointments.Application.Reports.Stores;
using MedicalAppointments.Domain.Appointments;
using MedicalAppointments.Domain.Scheduling;
using MedicalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedicalAppointments.Infrastructure.Reports.Persistence;

public sealed class EfAppointmentReportsStore : IAppointmentReportsStore
{
    private readonly AppDbContext _dbContext;

    public EfAppointmentReportsStore(AppDbContext dbContext) => _dbContext = dbContext;

    public async Task<AppointmentReportData> GetAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtcExclusive,
        CancellationToken cancellationToken = default)
    {
        var appointmentRows = await _dbContext.Appointments
            .AsNoTracking()
            .Where(x => x.StartAt >= fromUtc && x.StartAt < toUtcExclusive)
            .Select(x => new
            {
                x.Status,
                x.StartAt,
                x.EndAt,
                x.CreatedAt,
                x.DoctorId,
                x.ClinicId,
                DoctorName = x.Doctor.FirstName + " " + x.Doctor.LastName
            })
            .ToListAsync(cancellationToken);
        var appointments = appointmentRows.Select(x => new { x.Status, x.StartAt, x.DoctorId, x.DoctorName }).ToList();

        var doctorIds = appointments.Select(x => x.DoctorId).Distinct().ToArray();
        var doctorSpecialties = await _dbContext.DoctorSpecialties
            .AsNoTracking()
            .Where(x => doctorIds.Contains(x.DoctorId))
            .Select(x => new { x.DoctorId, x.SpecialtyId, SpecialtyName = x.Specialty.Name })
            .ToListAsync(cancellationToken);

        var statuses = appointments
            .GroupBy(x => x.Status)
            .Select(x => new AppointmentStatusCount(x.Key, x.Count()))
            .OrderBy(x => x.Status)
            .ToArray();

        var daily = appointments
            .GroupBy(x => DateOnly.FromDateTime(x.StartAt.UtcDateTime))
            .Select(x => new AppointmentDailyCount(x.Key, x.Count()))
            .OrderBy(x => x.DateUtc)
            .ToArray();

        var doctors = appointments
            .GroupBy(x => new { x.DoctorId, x.DoctorName })
            .Select(x => new AppointmentGroupCount(x.Key.DoctorId, x.Key.DoctorName, x.Count()))
            .OrderByDescending(x => x.Count).ThenBy(x => x.Name)
            .ToArray();

        var specialtyByDoctor = doctorSpecialties
            .GroupBy(x => x.DoctorId)
            .ToDictionary(x => x.Key, x => x.ToArray());
        var specialties = appointments
            .Where(x => specialtyByDoctor.ContainsKey(x.DoctorId))
            .SelectMany(x => specialtyByDoctor[x.DoctorId])
            .GroupBy(x => new { x.SpecialtyId, x.SpecialtyName })
            .Select(x => new AppointmentGroupCount(x.Key.SpecialtyId, x.Key.SpecialtyName, x.Count()))
            .OrderByDescending(x => x.Count).ThenBy(x => x.Name)
            .ToArray();

        var newPatients = await _dbContext.Patients.AsNoTracking()
            .CountAsync(x => x.CreatedAt >= fromUtc && x.CreatedAt < toUtcExclusive, cancellationToken);

        var reservedMinutes = appointmentRows
            .Where(x => x.Status is not AppointmentStatus.Cancelled and not AppointmentStatus.Rescheduled)
            .Sum(x => (int)(x.EndAt - x.StartAt).TotalMinutes);
        var appointmentRequests = await _dbContext.Appointments.AsNoTracking()
            .CountAsync(x => x.CreatedAt >= fromUtc && x.CreatedAt < toUtcExclusive, cancellationToken)
            + await _dbContext.WaitingListEntries.AsNoTracking()
                .CountAsync(x => x.CreatedAt >= fromUtc && x.CreatedAt < toUtcExclusive, cancellationToken);
        // Las citas provenientes de una oferta aceptada ya están representadas por su entrada en espera.
        var acceptedOffers = await _dbContext.WaitingListOffers.AsNoTracking()
            .Where(x => x.Status == MedicalAppointments.Domain.WaitingList.WaitingListOfferStatus.Accepted &&
                x.RespondedAt >= fromUtc && x.RespondedAt < toUtcExclusive)
            .Select(x => new { x.PatientId, x.DoctorId, x.ClinicId, x.SlotStartUtc, x.SlotEndUtc })
            .ToListAsync(cancellationToken);
        if (acceptedOffers.Count > 0)
        {
            var appointmentsFromAcceptedOffers = await _dbContext.Appointments.AsNoTracking()
                .Where(x => x.CreatedAt >= fromUtc && x.CreatedAt < toUtcExclusive)
                .Select(x => new { x.PatientId, x.DoctorId, x.ClinicId, x.StartAt, x.EndAt })
                .ToListAsync(cancellationToken);
            var duplicatedAcceptedBookings = appointmentsFromAcceptedOffers.Count(a => acceptedOffers.Any(o =>
                o.PatientId == a.PatientId && o.DoctorId == a.DoctorId && o.ClinicId == a.ClinicId &&
                o.SlotStartUtc == a.StartAt && o.SlotEndUtc == a.EndAt));
            appointmentRequests -= duplicatedAcceptedBookings;
        }
        var availableMinutes = await CalculateAvailableMinutesAsync(fromUtc, toUtcExclusive, cancellationToken);

        return new AppointmentReportData(statuses, daily, doctors, specialties, newPatients, availableMinutes, reservedMinutes, appointmentRequests);
    }

    private async Task<int> CalculateAvailableMinutesAsync(DateTimeOffset fromUtc, DateTimeOffset toUtcExclusive, CancellationToken ct)
    {
        var schedules = await _dbContext.DoctorSchedules.AsNoTracking().Include(x => x.Days)
            .Where(x => x.IsActive && x.EffectiveFrom <= DateOnly.FromDateTime(toUtcExclusive.UtcDateTime).AddDays(1) &&
                (!x.EffectiveTo.HasValue || x.EffectiveTo >= DateOnly.FromDateTime(fromUtc.UtcDateTime).AddDays(-1)))
            .ToListAsync(ct);
        if (schedules.Count == 0) return 0;
        var clinicIds = schedules.Select(x => x.ClinicId).Distinct().ToArray();
        var clinics = await _dbContext.Clinics.AsNoTracking().Where(x => clinicIds.Contains(x.Id))
            .Select(x => new { x.Id, x.OrganizationId, x.TimeZoneId, x.IsActive }).ToDictionaryAsync(x => x.Id, ct);
        var exceptions = await _dbContext.ScheduleExceptions.AsNoTracking().Where(x => x.IsActive && clinicIds.Contains(x.ClinicId)).ToListAsync(ct);
        var holidays = await _dbContext.Holidays.AsNoTracking().Where(x => x.IsActive).ToListAsync(ct);
        var blocks = await _dbContext.Blockouts.AsNoTracking().Where(x => x.IsActive && x.DoctorId.HasValue &&
            x.StartAt < toUtcExclusive && x.EndAt > fromUtc).ToListAsync(ct);

        var total = 0;
        foreach (var group in schedules.GroupBy(x => new { x.DoctorId, x.ClinicId }))
        {
            if (!clinics.TryGetValue(group.Key.ClinicId, out var clinic) || !clinic.IsActive) continue;
            TimeZoneInfo zone;
            try { zone = string.IsNullOrWhiteSpace(clinic.TimeZoneId) ? TimeZoneInfo.Utc : TimeZoneInfo.FindSystemTimeZoneById(clinic.TimeZoneId); }
            catch (TimeZoneNotFoundException) { zone = TimeZoneInfo.Utc; }
            catch (InvalidTimeZoneException) { zone = TimeZoneInfo.Utc; }
            var first = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(fromUtc, zone).DateTime);
            var last = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(toUtcExclusive.AddTicks(-1), zone).DateTime);
            for (var date = first; date <= last; date = date.AddDays(1))
            {
                var schedule = group.Where(x => x.AppliesToDate(date)).OrderByDescending(x => x.IsDefault).ThenBy(x => x.CreatedAt).FirstOrDefault();
                if (schedule is null || holidays.Any(h => h.Date == date && h.AppliesTo(clinic.OrganizationId, clinic.Id))) continue;
                var exception = exceptions.Where(x => x.DoctorId == group.Key.DoctorId && x.ClinicId == group.Key.ClinicId && x.StartDate <= date && x.EndDate >= date).ToArray();
                if (exception.Any(x => x.IsAllDay)) continue;
                var day = schedule.Days.FirstOrDefault(x => x.DayOfWeek == date.DayOfWeek);
                if (day is null) continue;
                for (var localStart = day.StartTime; localStart.AddMinutes(schedule.SlotDurationMinutes) <= day.EndTime; localStart = localStart.AddMinutes(schedule.SlotDurationMinutes + schedule.BufferMinutes))
                {
                    var localEnd = localStart.AddMinutes(schedule.SlotDurationMinutes);
                    if (exception.Any(x => x.StartTime.HasValue && x.StartTime.Value < localEnd && x.EndTime!.Value > localStart)) continue;
                    var local = date.ToDateTime(localStart, DateTimeKind.Unspecified);
                    if (zone.IsInvalidTime(local) || zone.IsAmbiguousTime(local)) continue;
                    var start = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(local, zone), TimeSpan.Zero);
                    var endLocal = date.ToDateTime(localEnd, DateTimeKind.Unspecified);
                    if (zone.IsInvalidTime(endLocal) || zone.IsAmbiguousTime(endLocal)) continue;
                    var end = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(endLocal, zone), TimeSpan.Zero);
                    if (start < fromUtc || start >= toUtcExclusive) continue;
                    if (blocks.Any(b => b.DoctorId == group.Key.DoctorId && b.StartAt < end && b.EndAt > start)) continue;
                    total += schedule.SlotDurationMinutes;
                }
            }
        }
        return total;
    }
}
