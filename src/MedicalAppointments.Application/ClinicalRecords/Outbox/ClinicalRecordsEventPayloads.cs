using MedicalAppointments.Domain.ClinicalRecords;

namespace MedicalAppointments.Application.ClinicalRecords.Outbox;

public sealed record ClinicalNoteEventPayload(
    Guid NoteId,
    Guid AppointmentId,
    Guid DoctorId,
    Guid ClinicId,
    Guid PatientId,
    Guid AuthorUserId,
    ClinicalNoteCategory Category,
    bool IsFinal,
    Guid? CorrectsNoteId,
    string ChangeType,
    DateTimeOffset OccurredAt,
    string? CorrelationId);