using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.MedicalStructure.Resources;
using MedicalAppointments.Domain.MedicalStructure;
using MedicalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedicalAppointments.Infrastructure.MedicalStructure.Persistence;

public sealed class EfResourceStore : IResourceStore
{
    private readonly AppDbContext _dbContext;

    public EfResourceStore(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsNormalizedNameInClinicAsync(
        Guid clinicId,
        string normalizedName,
        Guid? excludeId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Resources.AnyAsync(
            x => x.ClinicId == clinicId &&
                 x.NormalizedName == normalizedName &&
                 (excludeId == null || x.Id != excludeId),
            cancellationToken);
    }

    public Task<bool> ExistsNormalizedCodeInClinicAsync(
        Guid clinicId,
        string normalizedCode,
        Guid? excludeId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Resources.AnyAsync(
            x => x.ClinicId == clinicId &&
                 x.NormalizedCode == normalizedCode &&
                 (excludeId == null || x.Id != excludeId),
            cancellationToken);
    }

    public async Task AddAsync(Resource resource, CancellationToken cancellationToken = default)
    {
        await _dbContext.Resources.AddAsync(resource, cancellationToken);
    }

    public Task<Resource?> GetTrackedByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Resources
            .Include(x => x.Clinic)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<PagedResult<Resource>> GetPagedAsync(
        ResourceQuery query,
        CancellationToken cancellationToken = default)
    {
        var q = _dbContext.Resources
            .AsNoTracking()
            .Include(x => x.Clinic)
            .AsQueryable();

        if (query.ClinicId.HasValue)
        {
            q = q.Where(x => x.ClinicId == query.ClinicId.Value);
        }

        if (query.Kind.HasValue)
        {
            q = q.Where(x => x.Kind == query.Kind.Value);
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