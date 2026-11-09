using MedicalAppointments.Domain.ClinicalRecords;

namespace MedicalAppointments.Contracts.ClinicalRecords;

public sealed record ClinicalAttachmentResponse(
    Guid Id,
    Guid NoteId,
    string FileName,
    string ContentType,
    long SizeBytes,
    ClinicalAttachmentScanStatus ScanStatus,
    DateTimeOffset? ScannedAt,
    Guid UploadedByUserId,
    DateTimeOffset CreatedAt);

public sealed record ClinicalHistoryItemResponse(
    Guid NoteId,
    Guid AppointmentId,
    DateOnly AppointmentLocalDate,
    DateTimeOffset AppointmentStartAtUtc,
    string DoctorFullName,
    string ClinicName,
    ClinicalNoteCategory Category,
    string Title,
    string Content,
    bool IsFinal,
    string AuthorDisplayName,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset? FinalizedAt,
    Guid? CorrectsNoteId,
    int AttachmentCount);