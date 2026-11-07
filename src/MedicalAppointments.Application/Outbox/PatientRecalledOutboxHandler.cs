using System.Text.Json;
using MedicalAppointments.Application.Realtime;
using MedicalAppointments.Application.WaitingRoom.Outbox;
using MedicalAppointments.Domain.Outbox;

namespace MedicalAppointments.Application.Outbox;

public sealed class PatientRecalledOutboxHandler : IOutboxHandler
{
    private readonly IRealtimePublisher _publisher;
    private readonly JsonSerializerOptions _jsonOptions;

    public PatientRecalledOutboxHandler(IRealtimePublisher publisher, JsonSerializerOptions jsonOptions)
    {
        _publisher = publisher;
        _jsonOptions = jsonOptions;
    }

    public string SupportedType => OutboxTypes.WaitingRoomPatientRecalled;

    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Deserialize<PatientRecalledPayload>(message.Payload, _jsonOptions)
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

        await _publisher.PublishToGroupsAsync(groups, RealtimeEvents.PatientRecalled, payload, cancellationToken);
    }
}