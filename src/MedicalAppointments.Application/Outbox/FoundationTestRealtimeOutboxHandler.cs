using System.Text.Json;
using MedicalAppointments.Application.Realtime;
using MedicalAppointments.Domain.Outbox;

namespace MedicalAppointments.Application.Outbox;

public sealed class FoundationTestRealtimeOutboxHandler : IOutboxHandler
{
    private readonly IRealtimePublisher _publisher;
    private readonly JsonSerializerOptions _jsonOptions;

    public FoundationTestRealtimeOutboxHandler(
        IRealtimePublisher publisher,
        JsonSerializerOptions jsonOptions)
    {
        _publisher = publisher;
        _jsonOptions = jsonOptions;
    }

    public string SupportedType => OutboxTypes.FoundationTestEvent;

    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Deserialize<FoundationTestEventPayload>(
            message.Payload,
            _jsonOptions);

        if (payload is null)
        {
            throw new InvalidOperationException($"Unable to deserialize payload for outbox message {message.Id}.");
        }

        await _publisher.PublishToGroupAsync(
            RealtimeGroups.TestFoundation,
            RealtimeEvents.TestEvent,
            payload,
            cancellationToken);

        if (payload.TargetUserId.HasValue)
        {
            await _publisher.PublishToUserAsync(
                payload.TargetUserId.Value,
                RealtimeEvents.TestEvent,
                payload,
                cancellationToken);
        }
    }
}