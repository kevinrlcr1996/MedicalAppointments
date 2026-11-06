namespace MedicalAppointments.Application.Scheduling.Holidays;

public sealed record HolidayDto(
    Guid Id,
    Guid? OrganizationId,
    Guid? ClinicId,
    DateOnly Date,
    string Name,
    string? Description,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateHolidayCommand(
    Guid? OrganizationId,
    Guid? ClinicId,
    DateOnly Date,
    string Name,
    string? Description,
    bool IsActive);

public sealed record UpdateHolidayCommand(
    Guid Id,
    Guid? OrganizationId,
    Guid? ClinicId,
    DateOnly Date,
    string Name,
    string? Description,
    bool IsActive);

public sealed record SetHolidayActiveCommand(
    Guid Id,
    bool IsActive);

public sealed record HolidayQuery(
    Guid? OrganizationId,
    Guid? ClinicId,
    bool? IsActive,
    int Page,
    int PageSize);