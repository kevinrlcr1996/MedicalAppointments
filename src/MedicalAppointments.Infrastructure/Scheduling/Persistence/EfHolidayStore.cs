using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.Scheduling.Holidays;
using MedicalAppointments.Domain.Scheduling;
using MedicalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedicalAppointments.Infrastructure.Scheduling.Persistence;

public sealed class EfHolidayStore : IHolidayStore
{
    private readonly AppDbContext _dbContext;

    public EfHolidayStore(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Holiday holiday, CancellationToken cancellationToken = default)
    {
        await _dbContext.Holidays.AddAsync(holiday, cancellationToken);
    }

    public Task<Holiday?> GetTrackedByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Holidays.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<bool> ExistsForDateAsync(
        Guid organizationId,
        Guid clinicId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Holidays
            .AnyAsync(
                x => x.IsActive
                    && x.Date == date
                    && (x.ClinicId == clinicId
                        || (x.OrganizationId == organizationId && x.ClinicId == null)
                        || (x.OrganizationId == null && x.ClinicId == null)),
                cancellationToken);
    }

    public async Task<PagedResult<Holiday>> GetPagedAsync(
        HolidayQuery query,
        CancellationToken cancellationToken = default)
    {
        var q = _dbContext.Holidays.AsNoTracking();

        if (query.OrganizationId.HasValue)
        {
            q = q.Where(x => x.OrganizationId == query.OrganizationId.Value);
        }

        if (query.ClinicId.HasValue)
        {
            q = q.Where(x => x.ClinicId == query.ClinicId.Value);
        }

        if (query.IsActive.HasValue)
        {
            q = q.Where(x => x.IsActive == query.IsActive.Value);
        }

        q = q.OrderByDescending(x => x.Date);

        return await q.ToPagedResultAsync(query.Page, query.PageSize, cancellationToken);
    }
}