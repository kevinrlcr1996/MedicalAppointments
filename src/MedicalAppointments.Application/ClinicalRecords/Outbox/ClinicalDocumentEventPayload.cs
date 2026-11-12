namespace MedicalAppointments.Application.ClinicalRecords.Outbox;

/// <summary>D7: PHI-free. NO fileName, NO storageKey, NO sha256, NO contenido del documento.</summary>
public sealed record ClinicalDocumentEventPayload(
    string DocumentId,
    string PrescriptionId,
    string PatientId,
    string GeneratedByUserId,
    long SizeBytes,
    string ContentType,
    string ChangeType,
    string GeneratedAt,
    string OccurredAt,
    string CorrelationId);