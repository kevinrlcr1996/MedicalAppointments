using System.Text.Json;
using MedicalAppointments.Application.Realtime;
using MedicalAppointments.Application.WaitingList.Outbox;
using MedicalAppointments.Domain.Outbox;

namespace MedicalAppointments.Application.Outbox;

public sealed class WaitingListOfferCreatedOutboxHandler : IOutboxHandler
{
    private readonly IRealtimePublisher _publisher;
    private readonly JsonSerializerOptions _jsonOptions;

    public WaitingListOfferCreatedOutboxHandler(IRealtimePublisher publisher, JsonSerializerOptions jsonOptions)
    {
        _publisher = publisher;
        _jsonOptions = jsonOptions;
    }

    public string SupportedType => OutboxTypes.WaitingListOfferCreated;

    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Deserialize<OfferCreatedPayload>(message.Payload, _jsonOptions)
            ?? throw new InvalidOperationException($"Unable to deserialize payload for outbox message {message.Id}.");

        var groups = new List<string>
        {
            RealtimeGroups.ForPatient(payload.PatientId),
            RealtimeGroups.ForDoctor(payload.DoctorId)
        };

        if (payload.ClinicId.HasValue)
        {
            groups.Add(RealtimeGroups.ForClinic(payload.ClinicId.Value));
        }

        if (payload.PatientUserId.HasValue)
        {
            groups.Add(RealtimeGroups.ForUser(payload.PatientUserId.Value));
        }

        await _publisher.PublishToGroupsAsync(groups, RealtimeEvents.WaitingListOfferCreated, payload, cancellationToken);
    }
}