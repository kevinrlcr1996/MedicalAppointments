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

    public void EnqueueVitalSignsEvent(
        Domain.ClinicalRecords.VitalSigns vitalSigns,
        Domain.Appointments.Appointment appointment,
        string changeType,
        DateTimeOffset now,
        string? correlationId)
    {
        var payload = new VitalSignsEventPayload(
            vitalSigns.Id, appointment.Id, appointment.DoctorId, appointment.ClinicId, appointment.PatientId,
            vitalSigns.AuthorUserId, vitalSigns.IsFinal, vitalSigns.CorrectsVitalSignsId,
            changeType, vitalSigns.RecordedAtUtc, now, correlationId);

        var type = changeType switch
        {
            "Created" => OutboxTypes.VitalSignsCreated,
            "Updated" => OutboxTypes.VitalSignsUpdated,
            "Finalized" => OutboxTypes.VitalSignsFinalized,
            _ => OutboxTypes.VitalSignsUpdated
        };

        var message = OutboxMessage.Create(type, JsonSerializer.Serialize(payload, _jsonOptions), now, now);
        _outboxWriter.Enqueue(message);
    }

    public void EnqueueDiagnosisEvent(
        Domain.ClinicalRecords.Diagnosis diagnosis,
        Domain.Appointments.Appointment appointment,
        string changeType,
        DateTimeOffset now,
        string? correlationId)
    {
        var payload = new DiagnosisEventPayload(
            diagnosis.Id, appointment.Id, appointment.DoctorId, appointment.ClinicId, appointment.PatientId,
            diagnosis.AuthorUserId, diagnosis.IsFinal, diagnosis.CorrectsDiagnosisId,
            changeType, diagnosis.RecordedAtUtc, now, correlationId);

        var type = changeType switch
        {
            "Created" => OutboxTypes.DiagnosisCreated,
            "Updated" => OutboxTypes.DiagnosisUpdated,
            "Finalized" => OutboxTypes.DiagnosisFinalized,
            _ => OutboxTypes.DiagnosisUpdated
        };

        var message = OutboxMessage.Create(type, JsonSerializer.Serialize(payload, _jsonOptions), now, now);
        _outboxWriter.Enqueue(message);
    }

    public void EnqueueAllergyEvent(
        Domain.ClinicalRecords.PatientAllergy allergy,
        string changeType,
        DateTimeOffset now,
        string? correlationId)
    {
        var payload = new PatientAllergyEventPayload(
            allergy.Id, allergy.PatientId, allergy.AuthorUserId, allergy.IsFinal, allergy.IsResolved,
            allergy.CorrectsAllergyId, changeType, allergy.RecordedAtUtc, now, correlationId);

        var type = changeType switch
        {
            "Created" => OutboxTypes.AllergyCreated,
            "Updated" => OutboxTypes.AllergyUpdated,
            "Finalized" => OutboxTypes.AllergyFinalized,
            "Resolved" or "Unresolved" => OutboxTypes.AllergyResolved,
            _ => OutboxTypes.AllergyUpdated
        };

        var message = OutboxMessage.Create(type, JsonSerializer.Serialize(payload, _jsonOptions), now, now);
        _outboxWriter.Enqueue(message);
    }

    public void EnqueueMedicationEvent(
        Domain.ClinicalRecords.PatientMedication medication,
        string changeType,
        DateTimeOffset now,
        string? correlationId)
    {
        var payload = new PatientMedicationEventPayload(
            medication.Id, medication.PatientId, medication.AuthorUserId, medication.IsFinal,
            medication.Status, medication.CorrectsMedicationId, changeType, medication.RecordedAtUtc, now, correlationId);

        var type = changeType switch
        {
            "Created" => OutboxTypes.MedicationCreated,
            "Updated" => OutboxTypes.MedicationUpdated,
            "Finalized" => OutboxTypes.MedicationFinalized,
            "StatusChanged" => OutboxTypes.MedicationStatusChanged,
            _ => OutboxTypes.MedicationUpdated
        };

        var message = OutboxMessage.Create(type, JsonSerializer.Serialize(payload, _jsonOptions), now, now);
        _outboxWriter.Enqueue(message);
    }
}