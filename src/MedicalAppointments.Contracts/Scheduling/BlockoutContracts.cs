namespace MedicalAppointments.Contracts.Scheduling;

public sealed record BlockoutResponse(
    Guid Id,
    Guid? DoctorId,
    Guid? ClinicId,
    Guid? RoomId,
    Guid? ResourceId,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    string? Reason,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateBlockoutRequest(
    Guid? DoctorId,
    Guid? ClinicId,
    Guid? RoomId,
    Guid? ResourceId,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    string? Reason,
    bool IsActive);

public sealed record UpdateBlockoutRequest(
    Guid? DoctorId,
    Guid? ClinicId,
    Guid? RoomId,
    Guid? ResourceId,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    string? Reason,
    bool IsActive);