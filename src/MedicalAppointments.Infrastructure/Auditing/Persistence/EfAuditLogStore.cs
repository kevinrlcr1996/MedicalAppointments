using MedicalAppointments.Application.Auditing;
using MedicalAppointments.Application.Auditing.Stores;
using MedicalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedicalAppointments.Infrastructure.Auditing.Persistence;

public sealed class EfAuditLogStore : IAuditLogStore
{
    private readonly AppDbContext _dbContext;

    public EfAuditLogStore(AppDbContext dbContext) => _dbContext = dbContext;

    public async Task<(IReadOnlyList<AuditLogItemDto> Items, int TotalCount)> GetPageAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtcExclusive,
        int page,
        int pageSize,
        string? action,
        string? entityName,
        Guid? userId,
        Guid? entityId,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AuditLogs.AsNoTracking()
            .Where(x => x.Timestamp >= fromUtc && x.Timestamp < toUtcExclusive);
        if (action is not null) query = query.Where(x => x.Action.Contains(action));
        if (entityName is not null) query = query.Where(x => x.EntityName == entityName);
        if (userId.HasValue) query = query.Where(x => x.UserId == userId.Value);
        if (entityId.HasValue) query = query.Where(x => x.EntityId == entityId.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.Timestamp)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AuditLogItemDto(x.Id, x.UserId, x.Action, x.EntityName, x.EntityId, x.Timestamp, x.CorrelationId))
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
