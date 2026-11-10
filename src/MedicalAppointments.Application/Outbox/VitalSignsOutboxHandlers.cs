using System.Text.Json;
using MedicalAppointments.Application.ClinicalRecords.Outbox;
using MedicalAppointments.Application.Realtime;
using MedicalAppointments.Domain.Outbox;

namespace MedicalAppointments.Application.Outbox;

public sealed class VitalSignsCreatedOutboxHandler : IOutboxHandler
{
    private readonly IRealtimePublisher _publisher;
    private readonly JsonSerializerOptions _jsonOptions;

    public VitalSignsCreatedOutboxHandler(IRealtimePublisher publisher, JsonSerializerOptions jsonOptions)
    {
        _publisher = publisher;
        _jsonOptions = jsonOptions;
    }

    public string SupportedType => OutboxTypes.VitalSignsCreated;

    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Deserialize<VitalSignsEventPayload>(message.Payload, _jsonOptions)
            ?? throw new InvalidOperationException($"Unable to deserialize vital-signs payload for {message.Id}.");

        var groups = new List<string>
        {
            RealtimeGroups.ForDoctor(payload.DoctorId),
            RealtimeGroups.ForClinic(payload.ClinicId),
            RealtimeGroups.ForPatient(payload.PatientId),
            RealtimeGroups.ForAppointment(payload.AppointmentId),
            RealtimeGroups.ForUser(payload.AuthorUserId)
        };

        await _publisher.PublishToGroupsAsync(groups, RealtimeEvents.VitalSignsUpdated, payload, cancellationToken);
    }
}

public sealed class VitalSignsUpdatedOutboxHandler : IOutboxHandler
{
    private readonly IRealtimePublisher _publisher;
    private readonly JsonSerializerOptions _jsonOptions;

    public VitalSignsUpdatedOutboxHandler(IRealtimePublisher publisher, JsonSerializerOptions jsonOptions)
    {
        _publisher = publisher;
        _jsonOptions = jsonOptions;
    }

    public string SupportedType => OutboxTypes.VitalSignsUpdated;

    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Deserialize<VitalSignsEventPayload>(message.Payload, _jsonOptions)
            ?? throw new InvalidOperationException($"Unable to deserialize vital-signs payload for {message.Id}.");

        var groups = new List<string>
        {
            RealtimeGroups.ForDoctor(payload.DoctorId),
            RealtimeGroups.ForClinic(payload.ClinicId),
            RealtimeGroups.ForPatient(payload.PatientId),
            RealtimeGroups.ForAppointment(payload.AppointmentId),
            RealtimeGroups.ForUser(payload.AuthorUserId)
        };

        await _publisher.PublishToGroupsAsync(groups, RealtimeEvents.VitalSignsUpdated, payload, cancellationToken);
    }
}

public sealed class VitalSignsFinalizedOutboxHandler : IOutboxHandler
{
    private readonly IRealtimePublisher _publisher;
    private readonly JsonSerializerOptions _jsonOptions;

    public VitalSignsFinalizedOutboxHandler(IRealtimePublisher publisher, JsonSerializerOptions jsonOptions)
    {
        _publisher = publisher;
        _jsonOptions = jsonOptions;
    }

    public string SupportedType => OutboxTypes.VitalSignsFinalized;

    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Deserialize<VitalSignsEventPayload>(message.Payload, _jsonOptions)
            ?? throw new InvalidOperationException($"Unable to deserialize vital-signs payload for {message.Id}.");

        var groups = new List<string>
        {
            RealtimeGroups.ForDoctor(payload.DoctorId),
            RealtimeGroups.ForClinic(payload.ClinicId),
            RealtimeGroups.ForPatient(payload.PatientId),
            RealtimeGroups.ForAppointment(payload.AppointmentId),
            RealtimeGroups.ForUser(payload.AuthorUserId)
        };

        await _publisher.PublishToGroupsAsync(groups, RealtimeEvents.VitalSignsUpdated, payload, cancellationToken);
    }
}