namespace MedicalAppointments.Application.Scheduling.Availability;

public sealed record AvailabilityQuery(
    Guid DoctorId,
    Guid ClinicId,
    DateOnly Date,
    int? SlotDurationMinutes);

public sealed record AvailabilitySlotDto(
    Guid DoctorId,
    Guid ClinicId,
    DateOnly Date,
    DateTimeOffset StartAtUtc,
    DateTimeOffset EndAtUtc,
    TimeOnly StartLocal,
    TimeOnly EndLocal,
    int DurationMinutes,
    string TimeZoneId);

public sealed record AvailabilityDayDto(
    Guid DoctorId,
    Guid ClinicId,
    DateOnly Date,
    string TimeZoneId,
    IReadOnlyList<AvailabilitySlotDto> Slots);