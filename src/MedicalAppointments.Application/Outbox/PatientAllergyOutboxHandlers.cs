using System.Text.Json;
using MedicalAppointments.Application.ClinicalRecords.Outbox;
using MedicalAppointments.Application.Realtime;
using MedicalAppointments.Domain.Outbox;

namespace MedicalAppointments.Application.Outbox;

// Patrón corregido 8d.1: selladas independientes con ctor REAL. El abstract con ctor
// parameterless dejaba _publisher null -> NRE. No se asume uniformidad de DI en jerarquías.
// Publican a patient:{id} y user:{authorUserId} (standing data de paciente: no hay
// appointment/doctor/clinic que scopear aquí; el lector se valida por clinical_records.read).

public sealed class AllergyCreatedOutboxHandler : IOutboxHandler
{
    private readonly IRealtimePublisher _publisher;
    private readonly JsonSerializerOptions _jsonOptions;
    public AllergyCreatedOutboxHandler(IRealtimePublisher publisher, JsonSerializerOptions jsonOptions) { _publisher = publisher; _jsonOptions = jsonOptions; }
    public string SupportedType => OutboxTypes.AllergyCreated;
    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Deserialize<PatientAllergyEventPayload>(message.Payload, _jsonOptions)
            ?? throw new InvalidOperationException($"Unable to deserialize allergy payload for {message.Id}.");
        var groups = new List<string> { RealtimeGroups.ForPatient(payload.PatientId), RealtimeGroups.ForUser(payload.AuthorUserId) };
        await _publisher.PublishToGroupsAsync(groups, RealtimeEvents.PatientAllergyUpdated, payload, cancellationToken);
    }
}

public sealed class AllergyUpdatedOutboxHandler : IOutboxHandler
{
    private readonly IRealtimePublisher _publisher;
    private readonly JsonSerializerOptions _jsonOptions;
    public AllergyUpdatedOutboxHandler(IRealtimePublisher publisher, JsonSerializerOptions jsonOptions) { _publisher = publisher; _jsonOptions = jsonOptions; }
    public string SupportedType => OutboxTypes.AllergyUpdated;
    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Deserialize<PatientAllergyEventPayload>(message.Payload, _jsonOptions)
            ?? throw new InvalidOperationException($"Unable to deserialize allergy payload for {message.Id}.");
        var groups = new List<string> { RealtimeGroups.ForPatient(payload.PatientId), RealtimeGroups.ForUser(payload.AuthorUserId) };
        await _publisher.PublishToGroupsAsync(groups, RealtimeEvents.PatientAllergyUpdated, payload, cancellationToken);
    }
}

public sealed class AllergyFinalizedOutboxHandler : IOutboxHandler
{
    private readonly IRealtimePublisher _publisher;
    private readonly JsonSerializerOptions _jsonOptions;
    public AllergyFinalizedOutboxHandler(IRealtimePublisher publisher, JsonSerializerOptions jsonOptions) { _publisher = publisher; _jsonOptions = jsonOptions; }
    public string SupportedType => OutboxTypes.AllergyFinalized;
    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Deserialize<PatientAllergyEventPayload>(message.Payload, _jsonOptions)
            ?? throw new InvalidOperationException($"Unable to deserialize allergy payload for {message.Id}.");
        var groups = new List<string> { RealtimeGroups.ForPatient(payload.PatientId), RealtimeGroups.ForUser(payload.AuthorUserId) };
        await _publisher.PublishToGroupsAsync(groups, RealtimeEvents.PatientAllergyUpdated, payload, cancellationToken);
    }
}

public sealed class AllergyResolvedOutboxHandler : IOutboxHandler
{
    private readonly IRealtimePublisher _publisher;
    private readonly JsonSerializerOptions _jsonOptions;
    public AllergyResolvedOutboxHandler(IRealtimePublisher publisher, JsonSerializerOptions jsonOptions) { _publisher = publisher; _jsonOptions = jsonOptions; }
    public string SupportedType => OutboxTypes.AllergyResolved;
    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Deserialize<PatientAllergyEventPayload>(message.Payload, _jsonOptions)
            ?? throw new InvalidOperationException($"Unable to deserialize allergy payload for {message.Id}.");
        var groups = new List<string> { RealtimeGroups.ForPatient(payload.PatientId), RealtimeGroups.ForUser(payload.AuthorUserId) };
        await _publisher.PublishToGroupsAsync(groups, RealtimeEvents.PatientAllergyUpdated, payload, cancellationToken);
    }
}