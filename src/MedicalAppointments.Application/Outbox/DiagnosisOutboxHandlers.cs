using System.Text.Json;
using MedicalAppointments.Application.ClinicalRecords.Outbox;
using MedicalAppointments.Application.Realtime;
using MedicalAppointments.Domain.Outbox;

namespace MedicalAppointments.Application.Outbox;

// 8d.2: replico LITERAL el patrón de ClinicalAttachmentUploadedOutboxHandler (8b) y de las tres
// selladas de vitales (8d.1 corregido). NO uso base abstract con ctor parameterless: ese patrón
// dejaba _publisher null en las derivadas -> NullReferenceException en PublishToGroupsAsync.
// La uniformidad de resolución de DI en jerarquías abstractas NO se asume.

public sealed class DiagnosisCreatedOutboxHandler : IOutboxHandler
{
    private readonly IRealtimePublisher _publisher;
    private readonly JsonSerializerOptions _jsonOptions;

    public DiagnosisCreatedOutboxHandler(IRealtimePublisher publisher, JsonSerializerOptions jsonOptions)
    {
        _publisher = publisher;
        _jsonOptions = jsonOptions;
    }

    public string SupportedType => OutboxTypes.DiagnosisCreated;

    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Deserialize<DiagnosisEventPayload>(message.Payload, _jsonOptions)
            ?? throw new InvalidOperationException($"Unable to deserialize diagnosis payload for {message.Id}.");

        var groups = new List<string>
        {
            RealtimeGroups.ForDoctor(payload.DoctorId),
            RealtimeGroups.ForClinic(payload.ClinicId),
            RealtimeGroups.ForPatient(payload.PatientId),
            RealtimeGroups.ForAppointment(payload.AppointmentId),
            RealtimeGroups.ForUser(payload.AuthorUserId)
        };

        await _publisher.PublishToGroupsAsync(groups, RealtimeEvents.DiagnosisUpdated, payload, cancellationToken);
    }
}

public sealed class DiagnosisUpdatedOutboxHandler : IOutboxHandler
{
    private readonly IRealtimePublisher _publisher;
    private readonly JsonSerializerOptions _jsonOptions;

    public DiagnosisUpdatedOutboxHandler(IRealtimePublisher publisher, JsonSerializerOptions jsonOptions)
    {
        _publisher = publisher;
        _jsonOptions = jsonOptions;
    }

    public string SupportedType => OutboxTypes.DiagnosisUpdated;

    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Deserialize<DiagnosisEventPayload>(message.Payload, _jsonOptions)
            ?? throw new InvalidOperationException($"Unable to deserialize diagnosis payload for {message.Id}.");

        var groups = new List<string>
        {
            RealtimeGroups.ForDoctor(payload.DoctorId),
            RealtimeGroups.ForClinic(payload.ClinicId),
            RealtimeGroups.ForPatient(payload.PatientId),
            RealtimeGroups.ForAppointment(payload.AppointmentId),
            RealtimeGroups.ForUser(payload.AuthorUserId)
        };

        await _publisher.PublishToGroupsAsync(groups, RealtimeEvents.DiagnosisUpdated, payload, cancellationToken);
    }
}

public sealed class DiagnosisFinalizedOutboxHandler : IOutboxHandler
{
    private readonly IRealtimePublisher _publisher;
    private readonly JsonSerializerOptions _jsonOptions;

    public DiagnosisFinalizedOutboxHandler(IRealtimePublisher publisher, JsonSerializerOptions jsonOptions)
    {
        _publisher = publisher;
        _jsonOptions = jsonOptions;
    }

    public string SupportedType => OutboxTypes.DiagnosisFinalized;

    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Deserialize<DiagnosisEventPayload>(message.Payload, _jsonOptions)
            ?? throw new InvalidOperationException($"Unable to deserialize diagnosis payload for {message.Id}.");

        var groups = new List<string>
        {
            RealtimeGroups.ForDoctor(payload.DoctorId),
            RealtimeGroups.ForClinic(payload.ClinicId),
            RealtimeGroups.ForPatient(payload.PatientId),
            RealtimeGroups.ForAppointment(payload.AppointmentId),
            RealtimeGroups.ForUser(payload.AuthorUserId)
        };

        await _publisher.PublishToGroupsAsync(groups, RealtimeEvents.DiagnosisUpdated, payload, cancellationToken);
    }
}