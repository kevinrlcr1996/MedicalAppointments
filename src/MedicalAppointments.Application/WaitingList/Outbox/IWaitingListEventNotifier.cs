using MedicalAppointments.Domain.WaitingList;

namespace MedicalAppointments.Application.WaitingList.Outbox;

public interface IWaitingListEventNotifier
{
    void EnqueueSlotFreed(
        Guid doctorId,
        Guid? clinicId,
        Guid? roomId,
        DateOnly localDate,
        DateTimeOffset slotStartUtc,
        DateTimeOffset slotEndUtc,
        DateTimeOffset now,
        string? correlationId);

    void EnqueueOfferCreated(
        WaitingListOffer offer,
        Guid? patientUserId,
        DateTimeOffset now,
        string? correlationId);

    void EnqueueUpdated(
        Guid? entryId,
        Guid? offerId,
        Guid? patientId,
        Guid? patientUserId,
        Guid doctorId,
        Guid? clinicId,
        string changeType,
        WaitingListEntryStatus? entryStatus,
        WaitingListOfferStatus? offerStatus,
        DateTimeOffset now,
        string? correlationId);
}