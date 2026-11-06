using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.WaitingList.Common;
using MedicalAppointments.Domain.WaitingList;

namespace MedicalAppointments.Application.WaitingList.Stores;

public interface IWaitingListOfferStore
{
    Task AddAsync(WaitingListOffer offer, CancellationToken cancellationToken = default);

    Task<WaitingListOffer?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsForSlotAsync(
        Guid entryId,
        DateTimeOffset slotStartUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WaitingListOffer>> GetPendingExpiredAsync(
        DateTimeOffset now,
        int take,
        CancellationToken cancellationToken = default);

    Task<PagedResult<WaitingListOffer>> GetPagedAsync(
        WaitingListOfferQuery query,
        IReadOnlyCollection<Guid>? patientIds,
        IReadOnlyCollection<Guid>? doctorIds,
        bool all,
        CancellationToken cancellationToken = default);
}