using MedicalAppointments.Application.Outbox;
using MedicalAppointments.Domain.Appointments;
using MedicalAppointments.Domain.ClinicalRecords;
using MedicalAppointments.Domain.Outbox;
using System.Text.Json;

namespace MedicalAppointments.Application.ClinicalRecords.Outbox;

public sealed class ClinicalRecordsEventNotifier : IClinicalRecordsEventNotifier
{
    private readonly IOutboxWriter _outboxWriter;
    private readonly JsonSerializerOptions _jsonOptions;

    public ClinicalRecordsEventNotifier(
        IOutboxWriter outboxWriter,
        JsonSerializerOptions jsonOptions)
    {
        _outboxWriter = outboxWriter;
        _jsonOptions = jsonOptions;
    }

    public void EnqueueNoteEvent(
        ClinicalNote note,
        Appointment appointment,
        string changeType,
        DateTimeOffset now,
        string? correlationId)
    {
        var type = changeType switch
        {
            "Created" => OutboxTypes.ClinicalNoteCreated,
            "Finalized" => OutboxTypes.ClinicalNoteFinalized,
            _ => OutboxTypes.ClinicalNoteUpdated
        };

        var payload = new ClinicalNoteEventPayload(
            note.Id,
            note.AppointmentId,
            appointment.DoctorId,
            appointment.ClinicId,
            appointment.PatientId,
            note.AuthorUserId,
            note.Category,
            note.IsFinal,
            note.CorrectsNoteId,
            changeType,
            now,
            correlationId);

        var serialized = JsonSerializer.Serialize(payload, _jsonOptions);

        var message = OutboxMessage.Create(
            type: type,
            payload: serialized,
            occurredAt: now,
            createdAt: now);

        _outboxWriter.Enqueue(message);
    }

    public void EnqueueAttachmentEvent(
        ClinicalAttachment attachment,
        Domain.Appointments.Appointment appointment,
        string changeType,
        DateTimeOffset now,
        string? correlationId)
    {
        var payload = new ClinicalAttachmentEventPayload(
            attachment.Id,
            attachment.NoteId,
            appointment.Id,
            appointment.DoctorId,
            appointment.ClinicId,
            appointment.PatientId,
            attachment.SizeBytes,
            attachment.ScanStatus,
            changeType,
            now,
            correlationId);

        var serialized = JsonSerializer.Serialize(payload, _jsonOptions);

        var message = OutboxMessage.Create(
            type: OutboxTypes.ClinicalAttachmentUploaded,
            payload: serialized,
            occurredAt: now,
            createdAt: now);

        _outboxWriter.Enqueue(message);
    }
}