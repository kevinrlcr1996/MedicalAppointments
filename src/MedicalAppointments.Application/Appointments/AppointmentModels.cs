using MedicalAppointments.Domain.Appointments;

namespace MedicalAppointments.Application.Appointments;

public sealed record AppointmentDto(
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

public sealed record AppointmentQuery(
    Guid? PatientId,
    Guid? DoctorId,
    Guid? ClinicId,
    Guid? RoomId,
    AppointmentStatus? Status,
    DateTimeOffset? FromUtc,
    DateTimeOffset? ToUtc,
    int Page,
    int PageSize);

public sealed record CreateAppointmentCommand(
    Guid PatientId,
    Guid DoctorId,
    Guid ClinicId,
    Guid? RoomId,
    DateOnly LocalDate,
    DateTimeOffset StartAtUtc,
    DateTimeOffset EndAtUtc,
    string? Reason,
    string? Notes);

public sealed record CancelAppointmentCommand(
    Guid AppointmentId,
    string Reason);

public sealed record RescheduleAppointmentCommand(
    Guid AppointmentId,
    DateOnly NewLocalDate,
    DateTimeOffset NewStartAtUtc,
    DateTimeOffset NewEndAtUtc,
    string? Reason);

public sealed record RescheduleAppointmentResult(
    AppointmentDto OldAppointment,
    AppointmentDto NewAppointment);