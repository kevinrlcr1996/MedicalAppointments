using MedicalAppointments.Domain.MedicalStructure;

namespace MedicalAppointments.Application.MedicalStructure.Resources;

public sealed record ResourceDto(
    Guid Id,
    Guid ClinicId,
    string ClinicName,
    string Name,
    string? Code,
    ResourceKind Kind,
    string? Description,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateResourceCommand(
    Guid ClinicId,
    string Name,
    string? Code,
    ResourceKind Kind,
    string? Description,
    bool IsActive);

public sealed record UpdateResourceCommand(
    Guid Id,
    string Name,
    string? Code,
    ResourceKind Kind,
    string? Description,
    bool IsActive);

public sealed record SetResourceActiveCommand(
    Guid Id,
    bool IsActive);

public sealed record ResourceQuery(
    Guid? ClinicId,
    ResourceKind? Kind,
    string? Search,
    bool? IsActive,
    int Page,
    int PageSize);