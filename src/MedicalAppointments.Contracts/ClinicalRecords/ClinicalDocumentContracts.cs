namespace MedicalAppointments.Contracts.ClinicalRecords;

public sealed record ClinicalDocumentResponse(
    string Id,
    string PrescriptionId,
    string FileName,
    string ContentType,
    long SizeBytes,
    string GeneratedByUserId,
    string GeneratedAt);

public sealed record ClinicalDocumentDto(
    Guid Id,
    Guid PrescriptionId,
    string FileName,
    string ContentType,
    long SizeBytes,
    Guid GeneratedByUserId,
    DateTimeOffset GeneratedAt);
