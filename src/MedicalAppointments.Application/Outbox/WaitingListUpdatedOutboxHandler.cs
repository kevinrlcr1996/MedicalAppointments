using System.Text.Json;
using MedicalAppointments.Application.Realtime;
using MedicalAppointments.Application.WaitingList.Outbox;
using MedicalAppointments.Domain.Outbox;

namespace MedicalAppointments.Application.Outbox;

public sealed class WaitingListUpdatedOutboxHandler : IOutboxHandler
{
    private readonly IRealtimePublisher _publisher;
    private readonly JsonSerializerOptions _jsonOptions;

    public WaitingListUpdatedOutboxHandler(IRealtimePublisher publisher, JsonSerializerOptions jsonOptions)
    {
        _publisher = publisher;
        _jsonOptions = jsonOptions;
    }

    public string SupportedType => OutboxTypes.WaitingListUpdated;

    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Deserialize<WaitingListUpdatedPayload>(message.Payload, _jsonOptions)
            ?? throw new InvalidOperationException($"Unable to deserialize payload for outbox message {message.Id}.");

        var groups = new List<string> { RealtimeGroups.ForDoctor(payload.DoctorId) };

        if (payload.ClinicId.HasValue)
        {
            groups.Add(RealtimeGroups.ForClinic(payload.ClinicId.Value));
        }

        if (payload.PatientId.HasValue)
        {
            groups.Add(RealtimeGroups.ForPatient(payload.PatientId.Value));
        }

        if (payload.PatientUserId.HasValue)
        {
            groups.Add(RealtimeGroups.ForUser(payload.PatientUserId.Value));
        }

        await _publisher.PublishToGroupsAsync(groups, RealtimeEvents.WaitingListUpdated, payload, cancellationToken);
    }
}