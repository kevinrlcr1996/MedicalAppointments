namespace MedicalAppointments.Application.ClinicalRecords.Outbox;

public sealed record PatientAllergyEventPayload(
    Guid AllergyId,
    Guid PatientId,
    Guid AuthorUserId,
    bool IsFinal,
    bool IsResolved,
    Guid? CorrectsAllergyId,
    string ChangeType,
    DateTimeOffset RecordedAtUtc,
    DateTimeOffset OccurredAt,
    string? CorrelationId);