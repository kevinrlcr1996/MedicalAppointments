using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.Scheduling.Blockouts;
using MedicalAppointments.Domain.Scheduling;
using MedicalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedicalAppointments.Infrastructure.Scheduling.Persistence;

public sealed class EfBlockoutStore : IBlockoutStore
{
    private readonly AppDbContext _dbContext;

    public EfBlockoutStore(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Blockout blockout, CancellationToken cancellationToken = default)
    {
        await _dbContext.Blockouts.AddAsync(blockout, cancellationToken);
    }

    public Task<Blockout?> GetTrackedByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Blockouts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Blockout>> GetDoctorOverlappingAsync(
        Guid doctorId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Blockouts
            .AsNoTracking()
            .Where(x => x.DoctorId == doctorId
                && x.IsActive
                && x.StartAt < toUtc
                && x.EndAt > fromUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Blockout>> GetRoomOverlappingAsync(
        Guid roomId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Blockouts
            .AsNoTracking()
            .Where(x => x.RoomId == roomId
                && x.IsActive
                && x.StartAt < toUtc
                && x.EndAt > fromUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<Blockout>> GetPagedAsync(
        BlockoutQuery query,
        CancellationToken cancellationToken = default)
    {
        var q = _dbContext.Blockouts.AsNoTracking();

        if (query.DoctorId.HasValue)
        {
            q = q.Where(x => x.DoctorId == query.DoctorId.Value);
        }

        if (query.ClinicId.HasValue)
        {
            q = q.Where(x => x.ClinicId == query.ClinicId.Value);
        }

        if (query.RoomId.HasValue)
        {
            q = q.Where(x => x.RoomId == query.RoomId.Value);
        }

        if (query.ResourceId.HasValue)
        {
            q = q.Where(x => x.ResourceId == query.ResourceId.Value);
        }

        if (query.IsActive.HasValue)
        {
            q = q.Where(x => x.IsActive == query.IsActive.Value);
        }

        q = q.OrderByDescending(x => x.StartAt);

        return await q.ToPagedResultAsync(query.Page, query.PageSize, cancellationToken);
    }
}
