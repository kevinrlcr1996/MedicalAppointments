namespace MedicalAppointments.Application.ClinicalRecords.Outbox;

public sealed record VitalSignsEventPayload(
    Guid VitalSignsId,
    Guid AppointmentId,
    Guid DoctorId,
    Guid ClinicId,
    Guid PatientId,
    Guid AuthorUserId,
    bool IsFinal,
    Guid? CorrectsVitalSignsId,
    string ChangeType,
    DateTimeOffset RecordedAtUtc,
    DateTimeOffset OccurredAt,
    string? CorrelationId);