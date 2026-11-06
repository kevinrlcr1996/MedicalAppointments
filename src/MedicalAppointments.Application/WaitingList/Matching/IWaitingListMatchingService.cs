using MedicalAppointments.Application.WaitingList.Outbox;

namespace MedicalAppointments.Application.WaitingList.Matching;

public interface IWaitingListMatchingService
{
    Task MatchAsync(SlotFreedPayload slot, CancellationToken cancellationToken = default);
}