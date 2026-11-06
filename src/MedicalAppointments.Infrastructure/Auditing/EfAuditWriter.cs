using MedicalAppointments.Application.Common.Audit;
using MedicalAppointments.Domain.Auditing;
using MedicalAppointments.Infrastructure.Persistence;

namespace MedicalAppointments.Infrastructure.Auditing;

public sealed class EfAuditWriter : IAuditWriter
{
    private readonly AppDbContext _dbContext;

    public EfAuditWriter(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Enqueue(AuditEntry entry)
    {
        var auditLog = AuditLog.Create(
            userId: entry.UserId,
            action: entry.Action,
            entityName: entry.EntityName,
            entityId: entry.EntityId,
            timestamp: entry.Timestamp,
            ipAddress: entry.IpAddress,
            userAgent: entry.UserAgent,
            oldValues: entry.OldValues,
            newValues: entry.NewValues,
            correlationId: entry.CorrelationId);

        _dbContext.AuditLogs.Add(auditLog);
    }
}