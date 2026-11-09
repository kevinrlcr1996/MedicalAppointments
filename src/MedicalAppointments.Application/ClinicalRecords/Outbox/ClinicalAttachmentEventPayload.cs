using MedicalAppointments.Domain.ClinicalRecords;

namespace MedicalAppointments.Application.ClinicalRecords.Outbox;

// METADATA ONLY: sin binario, sin StorageKey, sin Sha256. fileName/contentType/size son metadatos de archivo, no PHI clínica.
public sealed record ClinicalAttachmentEventPayload(
    Guid AttachmentId,
    Guid NoteId,
    Guid AppointmentId,
    Guid DoctorId,
    Guid ClinicId,
    Guid PatientId,
    string FileName,
    string ContentType,
    long SizeBytes,
    ClinicalAttachmentScanStatus ScanStatus,
    string ChangeType,
    DateTimeOffset OccurredAt,
    string? CorrelationId);