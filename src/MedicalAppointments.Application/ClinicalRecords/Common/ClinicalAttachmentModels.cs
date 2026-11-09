using MedicalAppointments.Domain.ClinicalRecords;

namespace MedicalAppointments.Application.ClinicalRecords.Common;

public sealed record ClinicalAttachmentDto(
    Guid Id,
    Guid NoteId,
    string FileName,
    string ContentType,
    long SizeBytes,
    ClinicalAttachmentScanStatus ScanStatus,
    DateTimeOffset? ScannedAt,
    Guid UploadedByUserId,
    DateTimeOffset CreatedAt);

public sealed record UploadClinicalAttachmentCommand(
    Guid NoteId,
    string FileName,
    string ContentType,
    long SizeBytes,
    Stream Content);