using MedicalAppointments.Domain.ClinicalRecords;

namespace MedicalAppointments.Application.ClinicalRecords.Common;

public sealed record ClinicalNoteDto(
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

public sealed record CreateClinicalNoteCommand(
    Guid AppointmentId,
    ClinicalNoteCategory Category,
    string Title,
    string Content,
    DateTimeOffset? OccurredAtUtc);

public sealed record UpdateClinicalNoteCommand(
    Guid NoteId,
    ClinicalNoteCategory Category,
    string Title,
    string Content);

public sealed record FinalizeClinicalNoteCommand(Guid NoteId);

public sealed record CorrectClinicalNoteCommand(
    Guid NoteId,
    ClinicalNoteCategory Category,
    string Title,
    string Content,
    DateTimeOffset? OccurredAtUtc);