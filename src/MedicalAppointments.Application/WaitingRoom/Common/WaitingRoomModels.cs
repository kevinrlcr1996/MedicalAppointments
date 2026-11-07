using MedicalAppointments.Domain.WaitingRoom;

namespace MedicalAppointments.Application.WaitingRoom.Common;

public sealed record WaitingRoomCardDto(
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

public sealed record WaitingRoomBoardDto(
    Guid DoctorId,
    string DoctorFullName,
    Guid ClinicId,
    string ClinicName,
    DateOnly LocalDate,
    IReadOnlyList<WaitingRoomCardDto> Queued,
    IReadOnlyList<WaitingRoomCardDto> Called,
    IReadOnlyList<WaitingRoomCardDto> InConsultation,
    IReadOnlyList<WaitingRoomCardDto> Skipped,
    IReadOnlyList<WaitingRoomCardDto> Completed);

public sealed record CheckInToRoomCommand(
    Guid AppointmentId,
    string? Note);

public sealed record CallNextCommand(
    Guid DoctorId,
    Guid ClinicId,
    DateOnly LocalDate,
    Guid? RoomId);

public sealed record AnswerCallCommand(Guid CallId);

public sealed record SkipCallCommand(Guid CallId, string? Reason);

public sealed record RecallCallCommand(Guid CallId);

public sealed record CompleteCallCommand(Guid CallId);

public sealed record WaitingRoomBoardQuery(
    Guid DoctorId,
    Guid ClinicId,
    DateOnly LocalDate);