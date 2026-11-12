namespace MedicalAppointments.Application.Auditing.Stores;

public interface IAuditLogStore
{
    Task<(IReadOnlyList<AuditLogItemDto> Items, int TotalCount)> GetPageAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtcExclusive,
        int page,
        int pageSize,
        string? action,
        string? entityName,
        Guid? userId,
        Guid? entityId,
        CancellationToken cancellationToken = default);
}
