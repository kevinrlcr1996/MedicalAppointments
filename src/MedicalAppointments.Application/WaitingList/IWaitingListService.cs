using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.WaitingList.Common;

namespace MedicalAppointments.Application.WaitingList;

public interface IWaitingListService
{
    Task<PagedResult<WaitingListEntryDto>> GetEntriesAsync(
        WaitingListEntryQuery query,
        CancellationToken cancellationToken = default);

    Task<WaitingListEntryDto> GetEntryAsync(Guid id, CancellationToken cancellationToken = default);

    Task<WaitingListEntryDto> CreateEntryAsync(
        CreateWaitingListEntryCommand command,
        CancellationToken cancellationToken = default);

    Task<WaitingListEntryDto> UpdateEntryAsync(
        UpdateWaitingListEntryCommand command,
        CancellationToken cancellationToken = default);

    Task<WaitingListEntryDto> CancelEntryAsync(
        CancelWaitingListEntryCommand command,
        CancellationToken cancellationToken = default);

    Task<PagedResult<WaitingListOfferDto>> GetOffersAsync(
        WaitingListOfferQuery query,
        CancellationToken cancellationToken = default);

    Task<WaitingListOfferDto> RespondOfferAsync(
        RespondOfferCommand command,
        CancellationToken cancellationToken = default);
}