namespace MedicalAppointments.Contracts.Scheduling;

public sealed record HolidayResponse(
    Guid Id,
    Guid? OrganizationId,
    Guid? ClinicId,
    DateOnly Date,
    string Name,
    string? Description,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateHolidayRequest(
    Guid? OrganizationId,
    Guid? ClinicId,
    DateOnly Date,
    string Name,
    string? Description,
    bool IsActive);

public sealed record UpdateHolidayRequest(
    Guid? OrganizationId,
    Guid? ClinicId,
    DateOnly Date,
    string Name,
    string? Description,
    bool IsActive);