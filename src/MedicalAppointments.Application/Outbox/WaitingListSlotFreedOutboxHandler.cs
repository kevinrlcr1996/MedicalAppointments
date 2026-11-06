using System.Text.Json;
using MedicalAppointments.Application.WaitingList.Matching;
using MedicalAppointments.Application.WaitingList.Outbox;
using MedicalAppointments.Domain.Outbox;

namespace MedicalAppointments.Application.Outbox;

// Trigger interno: NO publica SignalR. Convierte el evento persistido "slot freed"
// en una llamada al matching service (after-commit, fuera del write path de citas).
public sealed class WaitingListSlotFreedOutboxHandler : IOutboxHandler
{
    private readonly IWaitingListMatchingService _matchingService;
    private readonly JsonSerializerOptions _jsonOptions;

    public WaitingListSlotFreedOutboxHandler(
        IWaitingListMatchingService matchingService,
        JsonSerializerOptions jsonOptions)
    {
        _matchingService = matchingService;
        _jsonOptions = jsonOptions;
    }

    public string SupportedType => OutboxTypes.WaitingListSlotFreed;

    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Deserialize<SlotFreedPayload>(message.Payload, _jsonOptions)
            ?? throw new InvalidOperationException($"Unable to deserialize payload for outbox message {message.Id}.");

        await _matchingService.MatchAsync(payload, cancellationToken);
    }
}