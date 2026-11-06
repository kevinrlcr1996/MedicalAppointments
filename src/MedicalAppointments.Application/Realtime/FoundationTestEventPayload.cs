namespace MedicalAppointments.Application.Realtime;

public sealed record FoundationTestEventPayload(
    Guid Id,
    string Message,
    DateTimeOffset OccurredAt,
    string? CorrelationId,
    Guid? TargetUserId);