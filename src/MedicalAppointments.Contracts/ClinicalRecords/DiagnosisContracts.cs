using MedicalAppointments.Domain.ClinicalRecords;

namespace MedicalAppointments.Contracts.ClinicalRecords;

public sealed record DiagnosisResponse(
    Guid Id, Guid AppointmentId, Guid AuthorUserId, Guid AuthorDoctorId, string AuthorDisplayName,
    string Code, string DisplayTerm, DiagnosisCategory Category, DateOnly? OnsetDate, string? Note,
    bool IsFinal, DateTimeOffset RecordedAtUtc, DateTimeOffset? FinalizedAt, Guid? FinalizedByUserId,
    Guid? CorrectsDiagnosisId, string? CorrectsDiagnosisTerm, DateTimeOffset CreatedAt);

public sealed record CreateDiagnosisRequest(
    string Code, string DisplayTerm, DiagnosisCategory Category, DateOnly? OnsetDate, string? Note, DateTimeOffset? RecordedAtUtc);

public sealed record UpdateDiagnosisRequest(
    string Code, string DisplayTerm, DiagnosisCategory Category, DateOnly? OnsetDate, string? Note);

public sealed record CorrectDiagnosisRequest(
    string Code, string DisplayTerm, DiagnosisCategory Category, DateOnly? OnsetDate, string? Note, DateTimeOffset? RecordedAtUtc);