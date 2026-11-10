using MedicalAppointments.Domain.ClinicalRecords;

namespace MedicalAppointments.Application.ClinicalRecords.Common;

public sealed record PatientAllergyDto(
    Guid Id, Guid PatientId, Guid AuthorUserId, Guid AuthorDoctorId, string AuthorDisplayName,
    string Allergen, AllergenClass AllergenClass, AllergySeverity Severity, string? Reaction,
    DateOnly? OnsetDate, bool IsResolved, DateTimeOffset? ResolvedAt, Guid? ResolvedByUserId,
    bool IsFinal, DateTimeOffset RecordedAtUtc, DateTimeOffset? FinalizedAt, Guid? FinalizedByUserId,
    Guid? CorrectsAllergyId, string? CorrectsAllergyAllergen, DateTimeOffset CreatedAt);

public sealed record CreateAllergyCommand(
    Guid PatientId, string Allergen, AllergenClass AllergenClass, AllergySeverity Severity,
    string? Reaction, DateOnly? OnsetDate, DateTimeOffset? RecordedAtUtc);

public sealed record UpdateAllergyCommand(
    Guid Id, string Allergen, AllergenClass AllergenClass, AllergySeverity Severity,
    string? Reaction, DateOnly? OnsetDate);

public sealed record CorrectAllergyCommand(
    Guid OriginalId, Guid PatientId, string Allergen, AllergenClass AllergenClass, AllergySeverity Severity,
    string? Reaction, DateOnly? OnsetDate, DateTimeOffset? RecordedAtUtc);