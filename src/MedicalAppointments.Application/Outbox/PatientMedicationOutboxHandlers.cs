using System.Text.Json;
using MedicalAppointments.Application.ClinicalRecords.Outbox;
using MedicalAppointments.Application.Realtime;
using MedicalAppointments.Domain.Outbox;

namespace MedicalAppointments.Application.Outbox;

public sealed class MedicationCreatedOutboxHandler : IOutboxHandler
{
    private readonly IRealtimePublisher _publisher; private readonly JsonSerializerOptions _jsonOptions;
    public MedicationCreatedOutboxHandler(IRealtimePublisher publisher, JsonSerializerOptions jsonOptions) { _publisher = publisher; _jsonOptions = jsonOptions; }
    public string SupportedType => OutboxTypes.MedicationCreated;
    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Deserialize<PatientMedicationEventPayload>(message.Payload, _jsonOptions) ?? throw new InvalidOperationException($"Unable to deserialize medication payload for {message.Id}.");
        var groups = new List<string> { RealtimeGroups.ForPatient(payload.PatientId), RealtimeGroups.ForUser(payload.AuthorUserId) };
        await _publisher.PublishToGroupsAsync(groups, RealtimeEvents.PatientMedicationUpdated, payload, cancellationToken);
    }
}
public sealed class MedicationUpdatedOutboxHandler : IOutboxHandler
{
    private readonly IRealtimePublisher _publisher; private readonly JsonSerializerOptions _jsonOptions;
    public MedicationUpdatedOutboxHandler(IRealtimePublisher publisher, JsonSerializerOptions jsonOptions) { _publisher = publisher; _jsonOptions = jsonOptions; }
    public string SupportedType => OutboxTypes.MedicationUpdated;
    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Deserialize<PatientMedicationEventPayload>(message.Payload, _jsonOptions) ?? throw new InvalidOperationException($"Unable to deserialize medication payload for {message.Id}.");
        var groups = new List<string> { RealtimeGroups.ForPatient(payload.PatientId), RealtimeGroups.ForUser(payload.AuthorUserId) };
        await _publisher.PublishToGroupsAsync(groups, RealtimeEvents.PatientMedicationUpdated, payload, cancellationToken);
    }
}
public sealed class MedicationFinalizedOutboxHandler : IOutboxHandler
{
    private readonly IRealtimePublisher _publisher; private readonly JsonSerializerOptions _jsonOptions;
    public MedicationFinalizedOutboxHandler(IRealtimePublisher publisher, JsonSerializerOptions jsonOptions) { _publisher = publisher; _jsonOptions = jsonOptions; }
    public string SupportedType => OutboxTypes.MedicationFinalized;
    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Deserialize<PatientMedicationEventPayload>(message.Payload, _jsonOptions) ?? throw new InvalidOperationException($"Unable to deserialize medication payload for {message.Id}.");
        var groups = new List<string> { RealtimeGroups.ForPatient(payload.PatientId), RealtimeGroups.ForUser(payload.AuthorUserId) };
        await _publisher.PublishToGroupsAsync(groups, RealtimeEvents.PatientMedicationUpdated, payload, cancellationToken);
    }
}
public sealed class MedicationStatusChangedOutboxHandler : IOutboxHandler
{
    private readonly IRealtimePublisher _publisher; private readonly JsonSerializerOptions _jsonOptions;
    public MedicationStatusChangedOutboxHandler(IRealtimePublisher publisher, JsonSerializerOptions jsonOptions) { _publisher = publisher; _jsonOptions = jsonOptions; }
    public string SupportedType => OutboxTypes.MedicationStatusChanged;
    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Deserialize<PatientMedicationEventPayload>(message.Payload, _jsonOptions) ?? throw new InvalidOperationException($"Unable to deserialize medication payload for {message.Id}.");
        var groups = new List<string> { RealtimeGroups.ForPatient(payload.PatientId), RealtimeGroups.ForUser(payload.AuthorUserId) };
        await _publisher.PublishToGroupsAsync(groups, RealtimeEvents.PatientMedicationUpdated, payload, cancellationToken);
    }
}