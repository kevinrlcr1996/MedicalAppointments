namespace MedicalAppointments.Application.Scheduling.Outbox;

public sealed record ScheduleChangedPayload(
    Guid ScheduleId,
    Guid DoctorId,
    Guid ClinicId,
    string ChangeType,
    DateOnly AffectedFrom,
    DateOnly AffectedTo,
    DateTimeOffset OccurredAt,
    string? CorrelationId);

public sealed record AvailabilityChangedPayload(
    Guid DoctorId,
    Guid ClinicId,
    DateOnly AffectedFrom,
    DateOnly AffectedTo,
    DateTimeOffset OccurredAt,
    string? CorrelationId);