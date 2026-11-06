using System.Text.Json;
using MedicalAppointments.Application.Realtime;
using MedicalAppointments.Application.Scheduling.Outbox;
using MedicalAppointments.Domain.Outbox;

namespace MedicalAppointments.Application.Outbox;

public sealed class ScheduleChangedOutboxHandler : IOutboxHandler
{
    private readonly IRealtimePublisher _publisher;
    private readonly JsonSerializerOptions _jsonOptions;

    public ScheduleChangedOutboxHandler(
        IRealtimePublisher publisher,
        JsonSerializerOptions jsonOptions)
    {
        _publisher = publisher;
        _jsonOptions = jsonOptions;
    }

    public string SupportedType => OutboxTypes.ScheduleChanged;

    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Deserialize<ScheduleChangedPayload>(
            message.Payload,
            _jsonOptions);

        if (payload is null)
        {
            throw new InvalidOperationException($"Unable to deserialize payload for outbox message {message.Id}.");
        }

        var groups = new[]
        {
            RealtimeGroups.ForDoctor(payload.DoctorId),
            RealtimeGroups.ForClinic(payload.ClinicId)
        };

        await _publisher.PublishToGroupsAsync(
            groups,
            RealtimeEvents.ScheduleChanged,
            payload,
            cancellationToken);
    }
}