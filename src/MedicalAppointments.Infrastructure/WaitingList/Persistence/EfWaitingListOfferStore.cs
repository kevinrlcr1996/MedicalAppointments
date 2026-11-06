using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.WaitingList.Common;
using MedicalAppointments.Application.WaitingList.Stores;
using MedicalAppointments.Domain.WaitingList;
using MedicalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedicalAppointments.Infrastructure.WaitingList.Persistence;

public sealed class EfWaitingListOfferStore : IWaitingListOfferStore
{
    private readonly AppDbContext _dbContext;

    public EfWaitingListOfferStore(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(WaitingListOffer offer, CancellationToken cancellationToken = default)
    {
        await _dbContext.WaitingListOffers.AddAsync(offer, cancellationToken);
    }

    public Task<WaitingListOffer?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.WaitingListOffers
            .Include(x => x.Doctor)
            .Include(x => x.Clinic)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<bool> ExistsForSlotAsync(
        Guid entryId,
        DateTimeOffset slotStartUtc,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.WaitingListOffers.AnyAsync(
            x => x.EntryId == entryId && x.SlotStartUtc == slotStartUtc, cancellationToken);
    }

    public async Task<IReadOnlyList<WaitingListOffer>> GetPendingExpiredAsync(
        DateTimeOffset now,
        int take,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.WaitingListOffers
            .Where(x => x.Status == WaitingListOfferStatus.Pending && x.ExpiresAt <= now)
            .OrderBy(x => x.ExpiresAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<WaitingListOffer>> GetPagedAsync(
        WaitingListOfferQuery query,
        IReadOnlyCollection<Guid>? patientIds,
        IReadOnlyCollection<Guid>? doctorIds,
        bool all,
        CancellationToken cancellationToken = default)
    {
        var q = _dbContext.WaitingListOffers
            .AsNoTracking()
            .Include(x => x.Doctor)
            .Include(x => x.Clinic)
            .AsQueryable();

        if (!all)
        {
            q = q.Where(x =>
                (patientIds != null && patientIds.Count > 0 && patientIds.Contains(x.PatientId)) ||
                (doctorIds != null && doctorIds.Count > 0 && doctorIds.Contains(x.DoctorId)));
        }

        if (query.PatientId.HasValue) q = q.Where(x => x.PatientId == query.PatientId.Value);
        if (query.DoctorId.HasValue) q = q.Where(x => x.DoctorId == query.DoctorId.Value);
        if (query.ClinicId.HasValue) q = q.Where(x => x.ClinicId == query.ClinicId.Value);
        if (query.Status.HasValue) q = q.Where(x => x.Status == query.Status.Value);

        q = q.OrderByDescending(x => x.CreatedAt);

        return await q.ToPagedResultAsync(query.Page, query.PageSize, cancellationToken);
    }
}