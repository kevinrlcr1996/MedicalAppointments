using MedicalAppointments.Contracts.Common;

namespace MedicalAppointments.Application.Auditing;

public interface IAuditLogService
{
    Task<PaginatedResponse<AuditLogItemDto>> GetPageAsync(AuditLogQuery query, CancellationToken cancellationToken = default);
}
