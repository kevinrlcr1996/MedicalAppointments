using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.MedicalStructure.Specialties;
using MedicalAppointments.Domain.MedicalStructure;
using MedicalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedicalAppointments.Infrastructure.MedicalStructure.Persistence;

public sealed class EfSpecialtyStore : ISpecialtyStore
{
    private readonly AppDbContext _dbContext;

    public EfSpecialtyStore(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsNormalizedNameAsync(
        string normalizedName,
        Guid? excludeId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Specialties.AnyAsync(
            x => x.NormalizedName == normalizedName &&
                 (excludeId == null || x.Id != excludeId),
            cancellationToken);
    }

    public Task<bool> ExistsNormalizedCodeAsync(
        string normalizedCode,
        Guid? excludeId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Specialties.AnyAsync(
            x => x.NormalizedCode == normalizedCode &&
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

        var count = await _dbContext.Specialties
            .AsNoTracking()
            .CountAsync(x => idList.Contains(x.Id), cancellationToken);

        return count == idList.Length;
    }

    public async Task AddAsync(Specialty specialty, CancellationToken cancellationToken = default)
    {
        await _dbContext.Specialties.AddAsync(specialty, cancellationToken);
    }

    public Task<Specialty?> GetTrackedByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Specialties.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<PagedResult<Specialty>> GetPagedAsync(
        SpecialtyQuery query,
        CancellationToken cancellationToken = default)
    {
        var q = _dbContext.Specialties.AsNoTracking();

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