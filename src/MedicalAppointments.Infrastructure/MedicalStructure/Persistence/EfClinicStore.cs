using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.MedicalStructure.Clinics;
using MedicalAppointments.Domain.MedicalStructure;
using MedicalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedicalAppointments.Infrastructure.MedicalStructure.Persistence;

public sealed class EfClinicStore : IClinicStore
{
    private readonly AppDbContext _dbContext;

    public EfClinicStore(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsNormalizedNameInOrganizationAsync(
        Guid organizationId,
        string normalizedName,
        Guid? excludeId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Clinics.AnyAsync(
            x => x.OrganizationId == organizationId &&
                 x.NormalizedName == normalizedName &&
                 (excludeId == null || x.Id != excludeId),
            cancellationToken);
    }

    public Task<bool> ExistsNormalizedCodeInOrganizationAsync(
        Guid organizationId,
        string normalizedCode,
        Guid? excludeId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Clinics.AnyAsync(
            x => x.OrganizationId == organizationId &&
                 x.NormalizedCode == normalizedCode &&
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

        var count = await _dbContext.Clinics
            .AsNoTracking()
            .CountAsync(x => idList.Contains(x.Id), cancellationToken);

        return count == idList.Length;
    }

    public async Task AddAsync(Clinic clinic, CancellationToken cancellationToken = default)
    {
        await _dbContext.Clinics.AddAsync(clinic, cancellationToken);
    }

    public Task<Clinic?> GetTrackedByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Clinics
            .Include(x => x.Organization)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<PagedResult<Clinic>> GetPagedAsync(
        ClinicQuery query,
        CancellationToken cancellationToken = default)
    {
        var q = _dbContext.Clinics
            .AsNoTracking()
            .Include(x => x.Organization)
            .AsQueryable();

        if (query.OrganizationId.HasValue)
        {
            q = q.Where(x => x.OrganizationId == query.OrganizationId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToUpperInvariant();

            q = q.Where(x =>
                x.NormalizedName.Contains(search) ||
                (x.NormalizedCode != null && x.NormalizedCode.Contains(search)));
        }

        if (query.IsActive.HasValue)
        {
            q = q.Where(x => x.IsActive == query.IsActive.Value);
        }

        q = q.OrderBy(x => x.Name);

        return await q.ToPagedResultAsync(query.Page, query.PageSize, cancellationToken);
    }
}