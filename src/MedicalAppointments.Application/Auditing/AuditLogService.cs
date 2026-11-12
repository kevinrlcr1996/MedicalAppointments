using FluentValidation;
using MedicalAppointments.Application.Auditing.Stores;
using MedicalAppointments.Contracts.Common;

namespace MedicalAppointments.Application.Auditing;

public sealed class AuditLogService : IAuditLogService
{
    private readonly IAuditLogStore _store;
    private readonly IValidator<AuditLogQuery> _validator;

    public AuditLogService(IAuditLogStore store, IValidator<AuditLogQuery> validator)
    {
        _store = store;
        _validator = validator;
    }

    public async Task<PaginatedResponse<AuditLogItemDto>> GetPageAsync(AuditLogQuery query, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(query, cancellationToken);
        var fromUtc = new DateTimeOffset(query.FromDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var toUtcExclusive = new DateTimeOffset(query.ToDate.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var (items, totalCount) = await _store.GetPageAsync(
            fromUtc, toUtcExclusive, query.Page, query.PageSize,
            Clean(query.Action), Clean(query.EntityName), query.UserId, query.EntityId, cancellationToken);

        return new PaginatedResponse<AuditLogItemDto>(
            items, query.Page, query.PageSize, totalCount,
            (int)Math.Ceiling(totalCount / (double)query.PageSize));
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
