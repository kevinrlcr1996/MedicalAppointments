namespace MedicalAppointments.Application.ClinicalRecords.Common;

public sealed record ClinicalDocumentModel(
    Guid Id,
    Guid PrescriptionId,
    string FileName,
    string ContentType,
    long SizeBytes,
    Guid GeneratedByUserId,
    DateTimeOffset GeneratedAt);
