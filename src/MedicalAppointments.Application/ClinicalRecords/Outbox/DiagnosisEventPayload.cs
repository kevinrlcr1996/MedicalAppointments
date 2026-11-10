namespace MedicalAppointments.Application.ClinicalRecords.Outbox;

public sealed record DiagnosisEventPayload(
    Guid DiagnosisId,
    Guid AppointmentId,
    Guid DoctorId,
    Guid ClinicId,
    Guid PatientId,
    Guid AuthorUserId,
    bool IsFinal,
    Guid? CorrectsDiagnosisId,
    string ChangeType,
    DateTimeOffset RecordedAtUtc,
    DateTimeOffset OccurredAt,
    string? CorrelationId);