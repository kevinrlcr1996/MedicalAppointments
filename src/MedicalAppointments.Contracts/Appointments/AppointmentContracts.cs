using MedicalAppointments.Domain.Appointments;

namespace MedicalAppointments.Contracts.Appointments;

public sealed record AppointmentResponse(
    Guid Id,
    Guid PatientId,
    string PatientFullName,
    Guid DoctorId,
    string DoctorFullName,
    Guid ClinicId,
    string ClinicName,
    Guid? RoomId,
    string? RoomName,
    DateOnly LocalDate,
    DateTimeOffset StartAtUtc,
    DateTimeOffset EndAtUtc,
    AppointmentStatus Status,
    string? Reason,
    string? Notes,
    Guid? OriginalAppointmentId,
    DateTimeOffset? CancelledAt,
    Guid? CancelledByUserId,
    string? CancellationReason,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateAppointmentRequest(
    Guid PatientId,
    Guid DoctorId,
    Guid ClinicId,
    Guid? RoomId,
    DateOnly LocalDate,
    DateTimeOffset StartAtUtc,
    DateTimeOffset EndAtUtc,
    string? Reason,
    string? Notes);

public sealed record CancelAppointmentRequest(string Reason);

public sealed record RescheduleAppointmentRequest(
    DateOnly NewLocalDate,
    DateTimeOffset NewStartAtUtc,
    DateTimeOffset NewEndAtUtc,
    string? Reason);

public sealed record RescheduleAppointmentResponse(
    AppointmentResponse OldAppointment,
    AppointmentResponse NewAppointment);