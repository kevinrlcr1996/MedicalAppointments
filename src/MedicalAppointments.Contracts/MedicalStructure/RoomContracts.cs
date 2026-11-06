namespace MedicalAppointments.Contracts.MedicalStructure;

public sealed record RoomResponse(
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

public sealed record CreateRoomRequest(
    Guid ClinicId,
    string Name,
    string? Code,
    int? Capacity,
    string? Notes,
    bool IsActive);

public sealed record UpdateRoomRequest(
    string Name,
    string? Code,
    int? Capacity,
    string? Notes,
    bool IsActive);