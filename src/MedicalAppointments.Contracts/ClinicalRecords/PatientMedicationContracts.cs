using MedicalAppointments.Domain.ClinicalRecords;

namespace MedicalAppointments.Contracts.ClinicalRecords;

public sealed record PatientMedicationResponse(
    Guid Id, Guid PatientId, Guid AuthorUserId, Guid AuthorDoctorId, string AuthorDisplayName,
    string MedicationName, string? Strength, string? DosageForm, string? Frequency, string? Route,
    DateOnly? StartDate, string? Indication, MedicationStatus Status, DateTimeOffset? SuspendedAt,
    DateTimeOffset? DiscontinuedAt, Guid? StatusChangedByUserId, bool IsFinal, DateTimeOffset RecordedAtUtc,
    DateTimeOffset? FinalizedAt, Guid? FinalizedByUserId, Guid? CorrectsMedicationId, string? CorrectsMedicationName, DateTimeOffset CreatedAt);

public sealed record CreateMedicationRequest(
    string MedicationName, string? Strength, string? DosageForm, string? Frequency, string? Route,
    DateOnly? StartDate, string? Indication, DateTimeOffset? RecordedAtUtc);

public sealed record UpdateMedicationRequest(
    string MedicationName, string? Strength, string? DosageForm, string? Frequency, string? Route,
    DateOnly? StartDate, string? Indication);

public sealed record CorrectMedicationRequest(
    Guid PatientId, string MedicationName, string? Strength, string? DosageForm, string? Frequency,
    string? Route, DateOnly? StartDate, string? Indication, DateTimeOffset? RecordedAtUtc);