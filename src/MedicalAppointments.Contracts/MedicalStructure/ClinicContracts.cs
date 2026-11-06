namespace MedicalAppointments.Contracts.MedicalStructure;

public sealed record ClinicResponse(
    Guid Id,
    Guid OrganizationId,
    string OrganizationName,
    string Name,
    string? Code,
    string? Description,
    string? Address,
    string? City,
    string? Country,
    string? Phone,
    string? Email,
    string? TimeZoneId,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateClinicRequest(
    Guid OrganizationId,
    string Name,
    string? Code,
    string? Description,
    string? Address,
    string? City,
    string? Country,
    string? Phone,
    string? Email,
    string? TimeZoneId,
    bool IsActive);

public sealed record UpdateClinicRequest(
    string Name,
    string? Code,
    string? Description,
    string? Address,
    string? City,
    string? Country,
    string? Phone,
    string? Email,
    string? TimeZoneId,
    bool IsActive);