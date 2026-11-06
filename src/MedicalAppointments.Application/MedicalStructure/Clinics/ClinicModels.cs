namespace MedicalAppointments.Application.MedicalStructure.Clinics;

public sealed record ClinicDto(
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

public sealed record CreateClinicCommand(
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

public sealed record UpdateClinicCommand(
    Guid Id,
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

public sealed record SetClinicActiveCommand(
    Guid Id,
    bool IsActive);

public sealed record ClinicQuery(
    Guid? OrganizationId,
    string? Search,
    bool? IsActive,
    int Page,
    int PageSize);