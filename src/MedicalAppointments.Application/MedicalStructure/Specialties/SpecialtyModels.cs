using MedicalAppointments.Domain.MedicalStructure;

namespace MedicalAppointments.Application.MedicalStructure.Specialties;

public sealed record SpecialtyDto(
    Guid Id,
    string Name,
    string? Code,
    string? Description,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateSpecialtyCommand(
    string Name,
    string? Code,
    string? Description,
    bool IsActive);

public sealed record UpdateSpecialtyCommand(
    Guid Id,
    string Name,
    string? Code,
    string? Description,
    bool IsActive);

public sealed record SetSpecialtyActiveCommand(
    Guid Id,
    bool IsActive);

public sealed record SpecialtyQuery(
    string? Search,
    bool? IsActive,
    int Page,
    int PageSize);