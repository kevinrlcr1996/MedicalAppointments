namespace MedicalAppointments.Application.MedicalStructure.Organizations;

public sealed record OrganizationDto(
    Guid Id,
    string Name,
    string? LegalName,
    string? TaxId,
    string? Email,
    string? Phone,
    string? Address,
    string? City,
    string? Country,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateOrganizationCommand(
    string Name,
    string? LegalName,
    string? TaxId,
    string? Email,
    string? Phone,
    string? Address,
    string? City,
    string? Country,
    bool IsActive);

public sealed record UpdateOrganizationCommand(
    Guid Id,
    string Name,
    string? LegalName,
    string? TaxId,
    string? Email,
    string? Phone,
    string? Address,
    string? City,
    string? Country,
    bool IsActive);

public sealed record SetOrganizationActiveCommand(
    Guid Id,
    bool IsActive);

public sealed record OrganizationQuery(
    string? Search,
    bool? IsActive,
    int Page,
    int PageSize);