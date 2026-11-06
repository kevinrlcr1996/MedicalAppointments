using MedicalAppointments.Domain.MedicalStructure;

namespace MedicalAppointments.Contracts.MedicalStructure;

public sealed record ResourceResponse(
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

public sealed record CreateResourceRequest(
    Guid ClinicId,
    string Name,
    string? Code,
    ResourceKind Kind,
    string? Description,
    bool IsActive);

public sealed record UpdateResourceRequest(
    string Name,
    string? Code,
    ResourceKind Kind,
    string? Description,
    bool IsActive);