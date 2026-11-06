namespace MedicalAppointments.Application.Scheduling.Blockouts;

public sealed record BlockoutDto(
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

public sealed record CreateBlockoutCommand(
    Guid? DoctorId,
    Guid? ClinicId,
    Guid? RoomId,
    Guid? ResourceId,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    string? Reason,
    bool IsActive);

public sealed record UpdateBlockoutCommand(
    Guid Id,
    Guid? DoctorId,
    Guid? ClinicId,
    Guid? RoomId,
    Guid? ResourceId,
    DateTimeOffset StartAt,
    DateTimeOffset EndAt,
    string? Reason,
    bool IsActive);

public sealed record SetBlockoutActiveCommand(
    Guid Id,
    bool IsActive);

public sealed record BlockoutQuery(
    Guid? DoctorId,
    Guid? ClinicId,
    Guid? RoomId,
    Guid? ResourceId,
    bool? IsActive,
    int Page,
    int PageSize);