namespace MedicalAppointments.Contracts.MedicalStructure;

public sealed record SpecialtyResponse(
    Guid Id,
    string Name,
    string? Code,
    string? Description,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateSpecialtyRequest(
    string Name,
    string? Code,
    string? Description,
    bool IsActive);

public sealed record UpdateSpecialtyRequest(
    string Name,
    string? Code,
    string? Description,
    bool IsActive);