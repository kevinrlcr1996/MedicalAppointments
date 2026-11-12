using MedicalAppointments.Domain.ClinicalRecords;

namespace MedicalAppointments.Application.ClinicalRecords.Outbox;

public sealed record PrescriptionEventPayload(
    Guid PrescriptionId, Guid PatientId, Guid AuthorUserId, bool IsFinal, PrescriptionStatus Status,
    Guid? CorrectsPrescriptionId, Guid? RenewedFromPrescriptionId, string ChangeType,
    DateTimeOffset RecordedAtUtc, DateTimeOffset OccurredAt, string? CorrelationId);