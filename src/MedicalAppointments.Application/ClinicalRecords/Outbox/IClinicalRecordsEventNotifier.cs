using MedicalAppointments.Domain.Appointments;
using MedicalAppointments.Domain.ClinicalRecords;

namespace MedicalAppointments.Application.ClinicalRecords.Outbox;

public interface IClinicalRecordsEventNotifier
{
    void EnqueueNoteEvent(
        ClinicalNote note,
        Appointment appointment,
        string changeType,
        DateTimeOffset now,
        string? correlationId);

    void EnqueueAttachmentEvent(
        ClinicalAttachment attachment,
        Domain.Appointments.Appointment appointment,
        string changeType,
        DateTimeOffset now,
        string? correlationId);

    void EnqueueVitalSignsEvent(
        Domain.ClinicalRecords.VitalSigns vitalSigns,
        Domain.Appointments.Appointment appointment,
        string changeType,
        DateTimeOffset now,
        string? correlationId);

    void EnqueueDiagnosisEvent(
        Domain.ClinicalRecords.Diagnosis diagnosis,
        Domain.Appointments.Appointment appointment,
        string changeType,
        DateTimeOffset now,
        string? correlationId);

    void EnqueueAllergyEvent(
        Domain.ClinicalRecords.PatientAllergy allergy,
        string changeType,
        DateTimeOffset now,
        string? correlationId);

    void EnqueueMedicationEvent(
        Domain.ClinicalRecords.PatientMedication medication,
        string changeType,
        DateTimeOffset now,
        string? correlationId);

    void EnqueuePrescriptionEvent(
        Domain.ClinicalRecords.Prescription prescription,
        string changeType,
        DateTimeOffset now,
        string? correlationId);

    void EnqueueClinicalDocumentEvent(
        Domain.ClinicalRecords.ClinicalDocument document,
        Guid patientId,
        DateTimeOffset now,
        string? correlationId);
}
