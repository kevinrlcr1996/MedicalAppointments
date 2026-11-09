using System.Text.Json;
using MedicalAppointments.Application.ClinicalRecords.Outbox;
using MedicalAppointments.Application.Realtime;
using MedicalAppointments.Domain.Outbox;

namespace MedicalAppointments.Application.Outbox;

public sealed class ClinicalNoteCreatedOutboxHandler : IOutboxHandler
{
    private readonly IRealtimePublisher _publisher;
    private readonly JsonSerializerOptions _jsonOptions;

    public ClinicalNoteCreatedOutboxHandler(IRealtimePublisher publisher, JsonSerializerOptions jsonOptions)
    {
        _publisher = publisher;
        _jsonOptions = jsonOptions;
    }

    public string SupportedType => OutboxTypes.ClinicalNoteCreated;

    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Deserialize<ClinicalNoteEventPayload>(message.Payload, _jsonOptions)
            ?? throw new InvalidOperationException($"Unable to deserialize payload for outbox message {message.Id}.");

        var groups = new List<string>
        {
            RealtimeGroups.ForDoctor(payload.DoctorId),
            RealtimeGroups.ForClinic(payload.ClinicId),
            RealtimeGroups.ForPatient(payload.PatientId),
            RealtimeGroups.ForAppointment(payload.AppointmentId),
            RealtimeGroups.ForUser(payload.AuthorUserId)
        };

        await _publisher.PublishToGroupsAsync(groups, RealtimeEvents.ClinicalNoteUpdated, payload, cancellationToken);
    }
}

public sealed class ClinicalNoteUpdatedOutboxHandler : IOutboxHandler
{
    private readonly IRealtimePublisher _publisher;
    private readonly JsonSerializerOptions _jsonOptions;

    public ClinicalNoteUpdatedOutboxHandler(IRealtimePublisher publisher, JsonSerializerOptions jsonOptions)
    {
        _publisher = publisher;
        _jsonOptions = jsonOptions;
    }

    public string SupportedType => OutboxTypes.ClinicalNoteUpdated;

    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Deserialize<ClinicalNoteEventPayload>(message.Payload, _jsonOptions)
            ?? throw new InvalidOperationException($"Unable to deserialize payload for outbox message {message.Id}.");

        var groups = new List<string>
        {
            RealtimeGroups.ForDoctor(payload.DoctorId),
            RealtimeGroups.ForClinic(payload.ClinicId),
            RealtimeGroups.ForPatient(payload.PatientId),
            RealtimeGroups.ForAppointment(payload.AppointmentId),
            RealtimeGroups.ForUser(payload.AuthorUserId)
        };

        await _publisher.PublishToGroupsAsync(groups, RealtimeEvents.ClinicalNoteUpdated, payload, cancellationToken);
    }
}

public sealed class ClinicalNoteFinalizedOutboxHandler : IOutboxHandler
{
    private readonly IRealtimePublisher _publisher;
    private readonly JsonSerializerOptions _jsonOptions;

    public ClinicalNoteFinalizedOutboxHandler(IRealtimePublisher publisher, JsonSerializerOptions jsonOptions)
    {
        _publisher = publisher;
        _jsonOptions = jsonOptions;
    }

    public string SupportedType => OutboxTypes.ClinicalNoteFinalized;

    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Deserialize<ClinicalNoteEventPayload>(message.Payload, _jsonOptions)
            ?? throw new InvalidOperationException($"Unable to deserialize payload for outbox message {message.Id}.");

        var groups = new List<string>
        {
            RealtimeGroups.ForDoctor(payload.DoctorId),
            RealtimeGroups.ForClinic(payload.ClinicId),
            RealtimeGroups.ForPatient(payload.PatientId),
            RealtimeGroups.ForAppointment(payload.AppointmentId),
            RealtimeGroups.ForUser(payload.AuthorUserId)
        };

        await _publisher.PublishToGroupsAsync(groups, RealtimeEvents.ClinicalNoteUpdated, payload, cancellationToken);
    }
}