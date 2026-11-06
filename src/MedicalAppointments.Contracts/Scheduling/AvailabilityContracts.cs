namespace MedicalAppointments.Contracts.Scheduling;

public sealed record AvailabilitySlotResponse(
    Guid DoctorId,
    Guid ClinicId,
    DateOnly Date,
    DateTimeOffset StartAtUtc,
    DateTimeOffset EndAtUtc,
    TimeOnly StartLocal,
    TimeOnly EndLocal,
    int DurationMinutes,
    string TimeZoneId);

public sealed record AvailabilityDayResponse(
    Guid DoctorId,
    Guid ClinicId,
    DateOnly Date,
    string TimeZoneId,
    IReadOnlyList<AvailabilitySlotResponse> Slots);