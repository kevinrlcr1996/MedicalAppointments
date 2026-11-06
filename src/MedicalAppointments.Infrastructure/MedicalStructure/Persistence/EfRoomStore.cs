using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.MedicalStructure.Rooms;
using MedicalAppointments.Domain.MedicalStructure;
using MedicalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedicalAppointments.Infrastructure.MedicalStructure.Persistence;

public sealed class EfRoomStore : IRoomStore
{
    private readonly AppDbContext _dbContext;

    public EfRoomStore(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsNormalizedNameInClinicAsync(
        Guid clinicId,
        string normalizedName,
        Guid? excludeId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Rooms.AnyAsync(
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
        return _dbContext.Rooms.AnyAsync(
            x => x.ClinicId == clinicId &&
                 x.NormalizedCode == normalizedCode &&
                 (excludeId == null || x.Id != excludeId),
            cancellationToken);
    }

    public async Task AddAsync(Room room, CancellationToken cancellationToken = default)
    {
        await _dbContext.Rooms.AddAsync(room, cancellationToken);
    }

    public Task<Room?> GetTrackedByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Rooms
            .Include(x => x.Clinic)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<PagedResult<Room>> GetPagedAsync(
        RoomQuery query,
        CancellationToken cancellationToken = default)
    {
        var q = _dbContext.Rooms
            .AsNoTracking()
            .Include(x => x.Clinic)
            .AsQueryable();

        if (query.ClinicId.HasValue)
        {
            q = q.Where(x => x.ClinicId == query.ClinicId.Value);
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