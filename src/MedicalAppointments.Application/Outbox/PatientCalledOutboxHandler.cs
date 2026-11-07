using System.Text.Json;
using MedicalAppointments.Application.Realtime;
using MedicalAppointments.Application.WaitingRoom.Outbox;
using MedicalAppointments.Domain.Outbox;

namespace MedicalAppointments.Application.Outbox;

public sealed class PatientCalledOutboxHandler : IOutboxHandler
{
    private readonly IRealtimePublisher _publisher;
    private readonly JsonSerializerOptions _jsonOptions;

    public PatientCalledOutboxHandler(IRealtimePublisher publisher, JsonSerializerOptions jsonOptions)
    {
        _publisher = publisher;
        _jsonOptions = jsonOptions;
    }

    public string SupportedType => OutboxTypes.WaitingRoomPatientCalled;

    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Deserialize<PatientCalledPayload>(message.Payload, _jsonOptions)
            ?? throw new InvalidOperationException($"Unable to deserialize payload for outbox message {message.Id}.");

        var groups = new List<string>
        {
            RealtimeGroups.ForDoctor(payload.DoctorId),
            RealtimeGroups.ForClinic(payload.ClinicId)
        };

        if (payload.RoomId.HasValue)
        {
            groups.Add(RealtimeGroups.ForRoom(payload.RoomId.Value));
        }

        if (payload.PatientUserId.HasValue)
        {
            groups.Add(RealtimeGroups.ForUser(payload.PatientUserId.Value));
        }

        await _publisher.PublishToGroupsAsync(groups, RealtimeEvents.PatientCalled, payload, cancellationToken);
    }
}