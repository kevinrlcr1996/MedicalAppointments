using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.MedicalStructure.Organizations;
using MedicalAppointments.Domain.MedicalStructure;
using MedicalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedicalAppointments.Infrastructure.MedicalStructure.Persistence;

public sealed class EfOrganizationStore : IOrganizationStore
{
    private readonly AppDbContext _dbContext;

    public EfOrganizationStore(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsNormalizedNameAsync(
        string normalizedName,
        Guid? excludeId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Organizations.AnyAsync(
            x => x.NormalizedName == normalizedName &&
                 (excludeId == null || x.Id != excludeId),
            cancellationToken);
    }

    public Task<bool> ExistsNormalizedTaxIdAsync(
        string normalizedTaxId,
        Guid? excludeId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Organizations.AnyAsync(
            x => x.NormalizedTaxId == normalizedTaxId &&
                 (excludeId == null || x.Id != excludeId),
            cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToArray();

        if (idList.Length == 0)
        {
            return true;
        }

        var count = await _dbContext.Organizations
            .AsNoTracking()
            .CountAsync(x => idList.Contains(x.Id), cancellationToken);

        return count == idList.Length;
    }

    public async Task AddAsync(Organization organization, CancellationToken cancellationToken = default)
    {
        await _dbContext.Organizations.AddAsync(organization, cancellationToken);
    }

    public Task<Organization?> GetTrackedByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Organizations.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<PagedResult<Organization>> GetPagedAsync(
        OrganizationQuery query,
        CancellationToken cancellationToken = default)
    {
        var q = _dbContext.Organizations.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToUpperInvariant();

            q = q.Where(x =>
                x.NormalizedName.Contains(search) ||
                (x.NormalizedTaxId != null && x.NormalizedTaxId.Contains(search)));
        }

        if (query.IsActive.HasValue)
        {
            q = q.Where(x => x.IsActive == query.IsActive.Value);
        }

        q = q.OrderBy(x => x.Name);

        return await q.ToPagedResultAsync(query.Page, query.PageSize, cancellationToken);
    }
}