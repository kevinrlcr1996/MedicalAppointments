using MedicalAppointments.Domain.Appointments;

namespace MedicalAppointments.Application.Appointments.Outbox;

public sealed record AppointmentEventPayload(
    Guid AppointmentId,
    Guid PatientId,
    Guid? PatientUserId,
    Guid DoctorId,
    Guid ClinicId,
    Guid? RoomId,
    DateTimeOffset StartAtUtc,
    DateTimeOffset EndAtUtc,
    AppointmentStatus Status,
    DateTimeOffset OccurredAt,
    string? CorrelationId);

public sealed record AppointmentStatusChangedPayload(
    Guid AppointmentId,
    Guid PatientId,
    Guid? PatientUserId,
    Guid DoctorId,
    Guid ClinicId,
    Guid? RoomId,
    AppointmentStatus OldStatus,
    AppointmentStatus NewStatus,
    DateTimeOffset OccurredAt,
    string? CorrelationId);

public sealed record AppointmentCancelledPayload(
    Guid AppointmentId,
    Guid PatientId,
    Guid? PatientUserId,
    Guid DoctorId,
    Guid ClinicId,
    Guid? RoomId,
    AppointmentStatus OldStatus,
    DateTimeOffset CancelledAt,
    Guid? CancelledByUserId,
    DateTimeOffset OccurredAt,
    string? CorrelationId);

public sealed record AppointmentRescheduledPayload(
    Guid OldAppointmentId,
    Guid NewAppointmentId,
    Guid PatientId,
    Guid? PatientUserId,
    Guid DoctorId,
    Guid ClinicId,
    Guid? RoomId,
    AppointmentStatus OldStatus,
    DateTimeOffset OldStartAtUtc,
    DateTimeOffset NewStartAtUtc,
    DateTimeOffset NewEndAtUtc,
    DateTimeOffset OccurredAt,
    string? CorrelationId);