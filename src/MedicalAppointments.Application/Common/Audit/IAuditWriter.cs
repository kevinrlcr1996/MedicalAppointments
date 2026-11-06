namespace MedicalAppointments.Application.Common.Audit;

public interface IAuditWriter
{
    void Enqueue(AuditEntry entry);
}

public sealed record AuditEntry(
    Guid? UserId,
    string Action,
    string EntityName,
    Guid EntityId,
    DateTimeOffset Timestamp,
    string? IpAddress,
    string? UserAgent,
    string? OldValues,
    string? NewValues,
    string? CorrelationId);