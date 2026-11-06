namespace MedicalAppointments.Application.MedicalStructure.Rooms;

public sealed record RoomDto(
    Guid Id,
    Guid ClinicId,
    string ClinicName,
    string Name,
    string? Code,
    int? Capacity,
    string? Notes,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateRoomCommand(
    Guid ClinicId,
    string Name,
    string? Code,
    int? Capacity,
    string? Notes,
    bool IsActive);

public sealed record UpdateRoomCommand(
    Guid Id,
    string Name,
    string? Code,
    int? Capacity,
    string? Notes,
    bool IsActive);

public sealed record SetRoomActiveCommand(
    Guid Id,
    bool IsActive);

public sealed record RoomQuery(
    Guid? ClinicId,
    string? Search,
    bool? IsActive,
    int Page,
    int PageSize);