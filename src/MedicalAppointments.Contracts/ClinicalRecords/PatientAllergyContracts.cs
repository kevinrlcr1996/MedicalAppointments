using MedicalAppointments.Domain.ClinicalRecords;

namespace MedicalAppointments.Contracts.ClinicalRecords;

public sealed record PatientAllergyResponse(
    Guid Id, Guid PatientId, Guid AuthorUserId, Guid AuthorDoctorId, string AuthorDisplayName,
    string Allergen, AllergenClass AllergenClass, AllergySeverity Severity, string? Reaction,
    DateOnly? OnsetDate, bool IsResolved, DateTimeOffset? ResolvedAt, Guid? ResolvedByUserId,
    bool IsFinal, DateTimeOffset RecordedAtUtc, DateTimeOffset? FinalizedAt, Guid? FinalizedByUserId,
    Guid? CorrectsAllergyId, string? CorrectsAllergyAllergen, DateTimeOffset CreatedAt);

public sealed record CreateAllergyRequest(
    string Allergen, AllergenClass AllergenClass, AllergySeverity Severity, string? Reaction, DateOnly? OnsetDate, DateTimeOffset? RecordedAtUtc);

public sealed record UpdateAllergyRequest(
    string Allergen, AllergenClass AllergenClass, AllergySeverity Severity, string? Reaction, DateOnly? OnsetDate);

public sealed record CorrectAllergyRequest(
    Guid PatientId, string Allergen, AllergenClass AllergenClass, AllergySeverity Severity, string? Reaction, DateOnly? OnsetDate, DateTimeOffset? RecordedAtUtc);