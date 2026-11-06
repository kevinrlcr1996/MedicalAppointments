using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.Scheduling.ScheduleExceptions;
using MedicalAppointments.Domain.Scheduling;
using MedicalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedicalAppointments.Infrastructure.Scheduling.Persistence;

public sealed class EfScheduleExceptionStore : IScheduleExceptionStore
{
    private readonly AppDbContext _dbContext;

    public EfScheduleExceptionStore(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ScheduleException exception, CancellationToken cancellationToken = default)
    {
        await _dbContext.ScheduleExceptions.AddAsync(exception, cancellationToken);
    }

    public Task<ScheduleException?> GetTrackedByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ScheduleExceptions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ScheduleException>> GetForDateAsync(
        Guid doctorId,
        Guid clinicId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ScheduleExceptions
            .AsNoTracking()
            .Where(x => x.DoctorId == doctorId
                && x.ClinicId == clinicId
                && x.IsActive
                && x.StartDate <= date
                && x.EndDate >= date)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<ScheduleException>> GetPagedAsync(
        ScheduleExceptionQuery query,
        CancellationToken cancellationToken = default)
    {
        var q = _dbContext.ScheduleExceptions.AsNoTracking();

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

        q = q.OrderByDescending(x => x.StartDate);

        return await q.ToPagedResultAsync(query.Page, query.PageSize, cancellationToken);
    }
}