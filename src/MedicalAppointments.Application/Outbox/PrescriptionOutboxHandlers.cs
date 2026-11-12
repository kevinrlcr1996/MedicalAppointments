using System.Text.Json;
using MedicalAppointments.Application.ClinicalRecords.Outbox;
using MedicalAppointments.Application.Realtime;
using MedicalAppointments.Domain.Outbox;

namespace MedicalAppointments.Application.Outbox;

public sealed class PrescriptionCreatedOutboxHandler : IOutboxHandler
{
    private readonly IRealtimePublisher _publisher; private readonly JsonSerializerOptions _jsonOptions;
    public PrescriptionCreatedOutboxHandler(IRealtimePublisher publisher, JsonSerializerOptions jsonOptions) { _publisher = publisher; _jsonOptions = jsonOptions; }
    public string SupportedType => OutboxTypes.PrescriptionCreated;
    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Deserialize<PrescriptionEventPayload>(message.Payload, _jsonOptions) ?? throw new InvalidOperationException($"Unable to deserialize prescription payload for {message.Id}.");
        var groups = new List<string> { RealtimeGroups.ForPatient(payload.PatientId), RealtimeGroups.ForUser(payload.AuthorUserId) };
        await _publisher.PublishToGroupsAsync(groups, RealtimeEvents.PrescriptionUpdated, payload, cancellationToken);
    }
}
public sealed class PrescriptionUpdatedOutboxHandler : IOutboxHandler
{
    private readonly IRealtimePublisher _publisher; private readonly JsonSerializerOptions _jsonOptions;
    public PrescriptionUpdatedOutboxHandler(IRealtimePublisher publisher, JsonSerializerOptions jsonOptions) { _publisher = publisher; _jsonOptions = jsonOptions; }
    public string SupportedType => OutboxTypes.PrescriptionUpdated;
    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Deserialize<PrescriptionEventPayload>(message.Payload, _jsonOptions) ?? throw new InvalidOperationException($"Unable to deserialize prescription payload for {message.Id}.");
        var groups = new List<string> { RealtimeGroups.ForPatient(payload.PatientId), RealtimeGroups.ForUser(payload.AuthorUserId) };
        await _publisher.PublishToGroupsAsync(groups, RealtimeEvents.PrescriptionUpdated, payload, cancellationToken);
    }
}
public sealed class PrescriptionIssuedOutboxHandler : IOutboxHandler
{
    private readonly IRealtimePublisher _publisher; private readonly JsonSerializerOptions _jsonOptions;
    public PrescriptionIssuedOutboxHandler(IRealtimePublisher publisher, JsonSerializerOptions jsonOptions) { _publisher = publisher; _jsonOptions = jsonOptions; }
    public string SupportedType => OutboxTypes.PrescriptionIssued;
    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Deserialize<PrescriptionEventPayload>(message.Payload, _jsonOptions) ?? throw new InvalidOperationException($"Unable to deserialize prescription payload for {message.Id}.");
        var groups = new List<string> { RealtimeGroups.ForPatient(payload.PatientId), RealtimeGroups.ForUser(payload.AuthorUserId) };
        await _publisher.PublishToGroupsAsync(groups, RealtimeEvents.PrescriptionUpdated, payload, cancellationToken);
    }
}
public sealed class PrescriptionCorrectedOutboxHandler : IOutboxHandler
{
    private readonly IRealtimePublisher _publisher; private readonly JsonSerializerOptions _jsonOptions;
    public PrescriptionCorrectedOutboxHandler(IRealtimePublisher publisher, JsonSerializerOptions jsonOptions) { _publisher = publisher; _jsonOptions = jsonOptions; }
    public string SupportedType => OutboxTypes.PrescriptionCorrected;
    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Deserialize<PrescriptionEventPayload>(message.Payload, _jsonOptions) ?? throw new InvalidOperationException($"Unable to deserialize prescription payload for {message.Id}.");
        var groups = new List<string> { RealtimeGroups.ForPatient(payload.PatientId), RealtimeGroups.ForUser(payload.AuthorUserId) };
        await _publisher.PublishToGroupsAsync(groups, RealtimeEvents.PrescriptionUpdated, payload, cancellationToken);
    }
}
public sealed class PrescriptionRenewedOutboxHandler : IOutboxHandler
{
    private readonly IRealtimePublisher _publisher; private readonly JsonSerializerOptions _jsonOptions;
    public PrescriptionRenewedOutboxHandler(IRealtimePublisher publisher, JsonSerializerOptions jsonOptions) { _publisher = publisher; _jsonOptions = jsonOptions; }
    public string SupportedType => OutboxTypes.PrescriptionRenewed;
    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Deserialize<PrescriptionEventPayload>(message.Payload, _jsonOptions) ?? throw new InvalidOperationException($"Unable to deserialize prescription payload for {message.Id}.");
        var groups = new List<string> { RealtimeGroups.ForPatient(payload.PatientId), RealtimeGroups.ForUser(payload.AuthorUserId) };
        await _publisher.PublishToGroupsAsync(groups, RealtimeEvents.PrescriptionUpdated, payload, cancellationToken);
    }
}
public sealed class PrescriptionCancelledOutboxHandler : IOutboxHandler
{
    private readonly IRealtimePublisher _publisher; private readonly JsonSerializerOptions _jsonOptions;
    public PrescriptionCancelledOutboxHandler(IRealtimePublisher publisher, JsonSerializerOptions jsonOptions) { _publisher = publisher; _jsonOptions = jsonOptions; }
    public string SupportedType => OutboxTypes.PrescriptionCancelled;
    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Deserialize<PrescriptionEventPayload>(message.Payload, _jsonOptions) ?? throw new InvalidOperationException($"Unable to deserialize prescription payload for {message.Id}.");
        var groups = new List<string> { RealtimeGroups.ForPatient(payload.PatientId), RealtimeGroups.ForUser(payload.AuthorUserId) };
        await _publisher.PublishToGroupsAsync(groups, RealtimeEvents.PrescriptionUpdated, payload, cancellationToken);
    }
}

public sealed class PrescriptionDispensedOutboxHandler : IOutboxHandler
{
    private readonly IRealtimePublisher _publisher; private readonly JsonSerializerOptions _jsonOptions;
    public PrescriptionDispensedOutboxHandler(IRealtimePublisher publisher, JsonSerializerOptions jsonOptions) { _publisher = publisher; _jsonOptions = jsonOptions; }
    public string SupportedType => OutboxTypes.PrescriptionDispensed;
    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Deserialize<PrescriptionEventPayload>(message.Payload, _jsonOptions) ?? throw new InvalidOperationException($"Unable to deserialize prescription payload for {message.Id}.");
        await _publisher.PublishToGroupsAsync(new[] { RealtimeGroups.ForPatient(payload.PatientId), RealtimeGroups.ForUser(payload.AuthorUserId) }, RealtimeEvents.PrescriptionUpdated, payload, cancellationToken);
    }
}

public sealed class PrescriptionExpiredOutboxHandler : IOutboxHandler
{
    private readonly IRealtimePublisher _publisher; private readonly JsonSerializerOptions _jsonOptions;
    public PrescriptionExpiredOutboxHandler(IRealtimePublisher publisher, JsonSerializerOptions jsonOptions) { _publisher = publisher; _jsonOptions = jsonOptions; }
    public string SupportedType => OutboxTypes.PrescriptionExpired;
    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Deserialize<PrescriptionEventPayload>(message.Payload, _jsonOptions) ?? throw new InvalidOperationException($"Unable to deserialize prescription payload for {message.Id}.");
        await _publisher.PublishToGroupsAsync(new[] { RealtimeGroups.ForPatient(payload.PatientId), RealtimeGroups.ForUser(payload.AuthorUserId) }, RealtimeEvents.PrescriptionUpdated, payload, cancellationToken);
    }
}
