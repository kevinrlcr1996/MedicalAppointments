namespace MedicalAppointments.Application.ClinicalRecords.Outbox;

public sealed record PatientMedicationEventPayload(
    Guid MedicationId, Guid PatientId, Guid AuthorUserId, bool IsFinal,
    Domain.ClinicalRecords.MedicationStatus Status, Guid? CorrectsMedicationId,
    string ChangeType, DateTimeOffset RecordedAtUtc, DateTimeOffset OccurredAt, string? CorrelationId);