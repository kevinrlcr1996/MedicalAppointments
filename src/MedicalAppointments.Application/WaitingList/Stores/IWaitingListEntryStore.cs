using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.WaitingList.Common;
using MedicalAppointments.Domain.WaitingList;

namespace MedicalAppointments.Application.WaitingList.Stores;

public interface IWaitingListEntryStore
{
    Task AddAsync(WaitingListEntry entry, CancellationToken cancellationToken = default);

    Task<WaitingListEntry?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<WaitingListEntry?> GetWithPatientAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WaitingListEntry>> GetWaitingCandidatesAsync(
        Guid doctorId,
        Guid? clinicId,
        DateTimeOffset slotStartUtc,
        DateTimeOffset slotEndUtc,
        CancellationToken cancellationToken = default);

    Task<PagedResult<WaitingListEntry>> GetPagedAsync(
        WaitingListEntryQuery query,
        WaitingListEntryReadFilter filter,
        CancellationToken cancellationToken = default);
}