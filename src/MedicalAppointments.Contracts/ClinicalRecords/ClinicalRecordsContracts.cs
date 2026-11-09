using MedicalAppointments.Domain.ClinicalRecords;

namespace MedicalAppointments.Contracts.ClinicalRecords;

public sealed record ClinicalNoteResponse(
    Guid Id,
    Guid AppointmentId,
    Guid AuthorUserId,
    Guid AuthorDoctorId,
    string AuthorDisplayName,
    ClinicalNoteCategory Category,
    string Title,
    string Content,
    bool IsFinal,
    DateTimeOffset OccurredAtUtc,
    DateTimeOffset? FinalizedAt,
    Guid? FinalizedByUserId,
    Guid? CorrectsNoteId,
    string? CorrectsNoteTitle,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateClinicalNoteRequest(
    ClinicalNoteCategory Category,
    string Title,
    string Content,
    DateTimeOffset? OccurredAtUtc);

public sealed record UpdateClinicalNoteRequest(
    ClinicalNoteCategory Category,
    string Title,
    string Content);

public sealed record CorrectClinicalNoteRequest(
    ClinicalNoteCategory Category,
    string Title,
    string Content,
    DateTimeOffset? OccurredAtUtc);