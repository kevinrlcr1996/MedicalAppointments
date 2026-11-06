using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.Scheduling.DoctorSchedules;
using MedicalAppointments.Domain.Scheduling;
using MedicalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedicalAppointments.Infrastructure.Scheduling.Persistence;

public sealed class EfDoctorScheduleStore : IDoctorScheduleStore
{
    private readonly AppDbContext _dbContext;

    public EfDoctorScheduleStore(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(DoctorSchedule schedule, CancellationToken cancellationToken = default)
    {
        await _dbContext.DoctorSchedules.AddAsync(schedule, cancellationToken);
    }

    public Task<DoctorSchedule?> GetTrackedWithDaysAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.DoctorSchedules
            .Include(x => x.Days)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<DoctorSchedule?> GetActiveForDateAsync(
        Guid doctorId,
        Guid clinicId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.DoctorSchedules
            .AsNoTracking()
            .Include(x => x.Days)
            .Where(x => x.DoctorId == doctorId
                && x.ClinicId == clinicId
                && x.IsActive
                && x.EffectiveFrom <= date
                && (x.EffectiveTo == null || x.EffectiveTo >= date))
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResult<DoctorSchedule>> GetPagedAsync(
        DoctorScheduleQuery query,
        CancellationToken cancellationToken = default)
    {
        var q = _dbContext.DoctorSchedules
            .AsNoTracking()
            .Include(x => x.Days)
            .AsQueryable();

        if (query.DoctorId.HasValue)
        {
            q = q.Where(x => x.DoctorId == query.DoctorId.Value);
        }

        if (query.ClinicId.HasValue)
        {
            q = q.Where(x => x.ClinicId == query.ClinicId.Value);
        }

        if (query.IsActive.HasValue)
        {
            q = q.Where(x => x.IsActive == query.IsActive.Value);
        }

        q = q.OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Name);

        return await q.ToPagedResultAsync(query.Page, query.PageSize, cancellationToken);
    }
}