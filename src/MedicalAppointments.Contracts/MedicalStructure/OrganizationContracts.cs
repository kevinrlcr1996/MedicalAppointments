namespace MedicalAppointments.Contracts.MedicalStructure;

public sealed record OrganizationResponse(
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

public sealed record CreateOrganizationRequest(
    string Name,
    string? LegalName,
    string? TaxId,
    string? Email,
    string? Phone,
    string? Address,
    string? City,
    string? Country,
    bool IsActive);

public sealed record UpdateOrganizationRequest(
    string Name,
    string? LegalName,
    string? TaxId,
    string? Email,
    string? Phone,
    string? Address,
    string? City,
    string? Country,
    bool IsActive);