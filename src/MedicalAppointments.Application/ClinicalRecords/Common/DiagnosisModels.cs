using MedicalAppointments.Domain.ClinicalRecords;

namespace MedicalAppointments.Application.ClinicalRecords.Common;

public sealed record DiagnosisDto(
    Guid Id,
    Guid AppointmentId,
    Guid AuthorUserId,
    Guid AuthorDoctorId,
    string AuthorDisplayName,
    string Code,
    string DisplayTerm,
    DiagnosisCategory Category,
    DateOnly? OnsetDate,
    string? Note,
    bool IsFinal,
    DateTimeOffset RecordedAtUtc,
    DateTimeOffset? FinalizedAt,
    Guid? FinalizedByUserId,
    Guid? CorrectsDiagnosisId,
    string? CorrectsDiagnosisTerm,
    DateTimeOffset CreatedAt);

public sealed record CreateDiagnosisCommand(
    Guid AppointmentId, string Code, string DisplayTerm, DiagnosisCategory Category,
    DateOnly? OnsetDate, string? Note, DateTimeOffset? RecordedAtUtc);

public sealed record UpdateDiagnosisCommand(
    Guid Id, string Code, string DisplayTerm, DiagnosisCategory Category,
    DateOnly? OnsetDate, string? Note);

public sealed record CorrectDiagnosisCommand(
    Guid OriginalId, string Code, string DisplayTerm, DiagnosisCategory Category,
    DateOnly? OnsetDate, string? Note, DateTimeOffset? RecordedAtUtc);