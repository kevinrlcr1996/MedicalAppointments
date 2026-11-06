namespace MedicalAppointments.Domain.Auditing;

public sealed class AuditLog
{
    private AuditLog()
    {
    }

    public Guid Id { get; private set; }

    public Guid? UserId { get; private set; }

    public string Action { get; private set; } = null!;

    public string EntityName { get; private set; } = null!;

    public Guid EntityId { get; private set; }

    public DateTimeOffset Timestamp { get; private set; }

    public string? IpAddress { get; private set; }

    public string? UserAgent { get; private set; }

    public string? OldValues { get; private set; }

    public string? NewValues { get; private set; }

    public string? CorrelationId { get; private set; }

    public static AuditLog Create(
        Guid? userId,
        string action,
        string entityName,
        Guid entityId,
        DateTimeOffset timestamp,
        string? ipAddress,
        string? userAgent,
        string? oldValues,
        string? newValues,
        string? correlationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityName);

        return new AuditLog
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Action = action.Trim(),
            EntityName = entityName.Trim(),
            EntityId = entityId,
            Timestamp = timestamp,
            IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress.Trim(),
            UserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent.Trim(),
            OldValues = string.IsNullOrWhiteSpace(oldValues) ? null : oldValues,
            NewValues = string.IsNullOrWhiteSpace(newValues) ? null : newValues,
            CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId.Trim()
        };
    }
}