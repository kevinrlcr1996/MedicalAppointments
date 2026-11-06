using System.Text.Json;
using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.Outbox;
using MedicalAppointments.Domain.Outbox;
using MedicalAppointments.Domain.WaitingList;

namespace MedicalAppointments.Application.WaitingList.Outbox;

public sealed class WaitingListEventNotifier : IWaitingListEventNotifier
{
    private readonly IOutboxWriter _outboxWriter;
    private readonly JsonSerializerOptions _jsonOptions;

    public WaitingListEventNotifier(
        IOutboxWriter outboxWriter,
        JsonSerializerOptions jsonOptions)
    {
        _outboxWriter = outboxWriter;
        _jsonOptions = jsonOptions;
    }

    public void EnqueueSlotFreed(
        Guid doctorId,
        Guid? clinicId,
        Guid? roomId,
        DateOnly localDate,
        DateTimeOffset slotStartUtc,
        DateTimeOffset slotEndUtc,
        DateTimeOffset now,
        string? correlationId)
    {
        var payload = new SlotFreedPayload(
            doctorId, clinicId, roomId, localDate, slotStartUtc, slotEndUtc, now, correlationId);

        Enqueue(OutboxTypes.WaitingListSlotFreed, payload, now);
    }

    public void EnqueueOfferCreated(
        WaitingListOffer offer,
        Guid? patientUserId,
        DateTimeOffset now,
        string? correlationId)
    {
        var payload = new OfferCreatedPayload(
            offer.Id, offer.EntryId, offer.PatientId, patientUserId, offer.DoctorId, offer.ClinicId,
            offer.SlotStartUtc, offer.SlotEndUtc, offer.ExpiresAt, now, correlationId);

        Enqueue(OutboxTypes.WaitingListOfferCreated, payload, now);
    }

    public void EnqueueUpdated(
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
        string? correlationId)
    {
        var payload = new WaitingListUpdatedPayload(
            entryId, offerId, patientId, patientUserId, doctorId, clinicId,
            changeType, entryStatus, offerStatus, now, correlationId);

        Enqueue(OutboxTypes.WaitingListUpdated, payload, now);
    }

    private void Enqueue(string type, object payload, DateTimeOffset now)
    {
        var serialized = JsonSerializer.Serialize(payload, _jsonOptions);

        var message = OutboxMessage.Create(
            type: type,
            payload: serialized,
            occurredAt: now,
            createdAt: now);

        _outboxWriter.Enqueue(message);
    }
}