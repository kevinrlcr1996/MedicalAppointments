using MedicalAppointments.Domain.ClinicalRecords;

namespace MedicalAppointments.Application.ClinicalRecords.Common;

public sealed record PrescriptionDto(
    Guid Id,
    Guid PatientId,
    Guid AuthorUserId,
    Guid AuthorDoctorId,
    string AuthorDisplayName,
    string MedicationName,
    string? Strength,
    string? DosageForm,
    string? Frequency,
    string? Route,
    int? Quantity,
    int? RefillsAllowed,
    int? DaysSupply,
    string? Instructions,
    string? Note,
    PrescriptionStatus Status,
    bool IsFinal,
    DateTimeOffset RecordedAtUtc,
    DateTimeOffset? IssuedAt,
    Guid? IssuedByUserId,
    DateTimeOffset? CancelledAt,
    Guid? CancelledByUserId,
    Guid? CorrectsPrescriptionId,
    string? CorrectsPrescriptionName,
    Guid? RenewedFromPrescriptionId,
    DateTimeOffset CreatedAt);

public sealed record CreatePrescriptionCommand(
    Guid PatientId,
    string MedicationName,
    string? Strength,
    string? DosageForm,
    string? Frequency,
    string? Route,
    int? Quantity,
    int? RefillsAllowed,
    int? DaysSupply,
    string? Instructions,
    string? Note,
    DateTimeOffset? RecordedAtUtc);

public sealed record UpdatePrescriptionCommand(
    Guid Id,
    string MedicationName,
    string? Strength,
    string? DosageForm,
    string? Frequency,
    string? Route,
    int? Quantity,
    int? RefillsAllowed,
    int? DaysSupply,
    string? Instructions,
    string? Note);

public sealed record CorrectPrescriptionCommand(
    Guid OriginalId,
    Guid PatientId,
    string MedicationName,
    string? Strength,
    string? DosageForm,
    string? Frequency,
    string? Route,
    int? Quantity,
    int? RefillsAllowed,
    int? DaysSupply,
    string? Instructions,
    string? Note,
    DateTimeOffset? RecordedAtUtc);