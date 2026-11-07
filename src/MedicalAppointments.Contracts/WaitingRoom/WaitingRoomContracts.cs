using MedicalAppointments.Domain.WaitingRoom;

namespace MedicalAppointments.Contracts.WaitingRoom;

public sealed record WaitingRoomCardResponse(
    Guid CallId,
    Guid AppointmentId,
    int SequenceNumber,
    Guid PatientId,
    string PatientFullName,
    Guid DoctorId,
    string DoctorFullName,
    Guid ClinicId,
    string ClinicName,
    Guid? RoomId,
    string? RoomName,
    DateOnly LocalDate,
    WaitingRoomCallStatus Status,
    int RecallCount,
    DateTimeOffset? CalledAt,
    DateTimeOffset? AnsweredAt,
    DateTimeOffset? SkippedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset AppointmentStartAtUtc,
    DateTimeOffset QueuedAt,
    string? Note);

public sealed record WaitingRoomBoardResponse(
    Guid DoctorId,
    string DoctorFullName,
    Guid ClinicId,
    string ClinicName,
    DateOnly LocalDate,
    IReadOnlyList<WaitingRoomCardResponse> Queued,
    IReadOnlyList<WaitingRoomCardResponse> Called,
    IReadOnlyList<WaitingRoomCardResponse> InConsultation,
    IReadOnlyList<WaitingRoomCardResponse> Skipped,
    IReadOnlyList<WaitingRoomCardResponse> Completed);

public sealed record CheckInToRoomRequest(string? Note);
public sealed record CallNextRequest(Guid DoctorId, Guid ClinicId, DateOnly LocalDate, Guid? RoomId);
public sealed record SkipCallRequest(string? Reason);