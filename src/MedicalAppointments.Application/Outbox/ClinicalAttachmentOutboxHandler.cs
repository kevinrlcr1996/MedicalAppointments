using System.Text.Json;
using MedicalAppointments.Application.ClinicalRecords.Outbox;
using MedicalAppointments.Application.Realtime;
using MedicalAppointments.Domain.Outbox;

namespace MedicalAppointments.Application.Outbox;

public sealed class ClinicalAttachmentUploadedOutboxHandler : IOutboxHandler
{
    private readonly IRealtimePublisher _publisher;
    private readonly JsonSerializerOptions _jsonOptions;

    public ClinicalAttachmentUploadedOutboxHandler(IRealtimePublisher publisher, JsonSerializerOptions jsonOptions)
    {
        _publisher = publisher;
        _jsonOptions = jsonOptions;
    }

    public string SupportedType => OutboxTypes.ClinicalAttachmentUploaded;

    public async Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Deserialize<ClinicalAttachmentEventPayload>(message.Payload, _jsonOptions)
            ?? throw new InvalidOperationException($"Unable to deserialize payload for outbox message {message.Id}.");

        var groups = new List<string>
        {
            RealtimeGroups.ForDoctor(payload.DoctorId),
            RealtimeGroups.ForClinic(payload.ClinicId),
            RealtimeGroups.ForPatient(payload.PatientId),
            RealtimeGroups.ForAppointment(payload.AppointmentId),
            RealtimeGroups.ForUser(payload.PatientId) // no hay userId del uploader en el payload; publicamos a los grupos de la cita
        };

        await _publisher.PublishToGroupsAsync(groups, RealtimeEvents.ClinicalAttachmentUpdated, payload, cancellationToken);
    }
}