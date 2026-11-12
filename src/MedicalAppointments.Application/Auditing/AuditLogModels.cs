namespace MedicalAppointments.Application.Auditing;

public sealed record AuditLogQuery(
    DateOnly FromDate,
    DateOnly ToDate,
    int Page = 1,
    int PageSize = 25,
    string? Action = null,
    string? EntityName = null,
    Guid? UserId = null,
    Guid? EntityId = null);

public sealed record AuditLogItemDto(
    Guid Id,
    Guid? UserId,
    string Action,
    string EntityName,
    Guid EntityId,
    DateTimeOffset Timestamp,
    string? CorrelationId);
