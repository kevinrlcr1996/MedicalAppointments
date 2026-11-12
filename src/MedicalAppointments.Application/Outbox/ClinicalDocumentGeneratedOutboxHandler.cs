using MedicalAppointments.Application.ClinicalRecords.Outbox;
using MedicalAppointments.Application.Realtime;
using MedicalAppointments.Domain.Outbox;
using System.Text.Json;

namespace MedicalAppointments.Application.Outbox;

public sealed class ClinicalDocumentGeneratedOutboxHandler : IOutboxHandler
{
    private readonly IRealtimePublisher _publisher;
    private readonly JsonSerializerOptions _jsonOptions;

    public ClinicalDocumentGeneratedOutboxHandler(IRealtimePublisher publisher, JsonSerializerOptions jsonOptions)
    {
        _publisher = publisher;
        _jsonOptions = jsonOptions;
    }

    public string SupportedType => OutboxTypes.DocumentGenerated;

    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<ClinicalDocumentEventPayload>(message.Payload, _jsonOptions);
        if (payload is null) return;

        var groups = new[]
        {
            $"patient:{payload.PatientId}",
            $"user:{payload.GeneratedByUserId}",
            // doctor:{id} del autor = DECISION (D7), no asumido.
        };

        await _publisher.PublishToGroupsAsync(groups, "ClinicalDocumentUpdated", payload, cancellationToken);
    }
}
