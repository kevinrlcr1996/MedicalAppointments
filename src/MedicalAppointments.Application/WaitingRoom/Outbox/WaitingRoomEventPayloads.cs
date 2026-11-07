using MedicalAppointments.Domain.WaitingRoom;

namespace MedicalAppointments.Application.WaitingRoom.Outbox;

public sealed record WaitingRoomUpdatedPayload(
    Guid CallId,
    Guid AppointmentId,
    Guid DoctorId,
    Guid ClinicId,
    Guid? RoomId,
    DateOnly LocalDate,
    int SequenceNumber,
    WaitingRoomCallStatus Status,
    string ChangeType,
    DateTimeOffset OccurredAt,
    string? CorrelationId);

public sealed record PatientCalledPayload(
    Guid CallId,
    Guid AppointmentId,
    Guid PatientId,
    Guid? PatientUserId,
    string PatientFullName,
    int SequenceNumber,
    Guid DoctorId,
    string DoctorFullName,
    Guid ClinicId,
    Guid? RoomId,
    string? RoomName,
    DateTimeOffset OccurredAt,
    string? CorrelationId);

public sealed record PatientRecalledPayload(
    Guid CallId,
    Guid AppointmentId,
    Guid PatientId,
    Guid? PatientUserId,
    string PatientFullName,
    int SequenceNumber,
    int RecallCount,
    Guid DoctorId,
    Guid ClinicId,
    Guid? RoomId,
    string? RoomName,
    DateTimeOffset OccurredAt,
    string? CorrelationId);