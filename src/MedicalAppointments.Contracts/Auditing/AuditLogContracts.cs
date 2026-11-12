namespace MedicalAppointments.Contracts.Auditing;

public sealed record AuditLogResponse(
    Guid Id,
    Guid? UserId,
    string Action,
    string EntityName,
    Guid EntityId,
    DateTimeOffset Timestamp,
    string? CorrelationId);
