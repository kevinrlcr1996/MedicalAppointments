using MedicalAppointments.Domain.MedicalStructure;

namespace MedicalAppointments.Application.MedicalStructure.Doctors;

public sealed record DoctorSpecialtyDto(
    Guid SpecialtyId,
    string Name,
    string? Code);

public sealed record DoctorClinicDto(
    Guid ClinicId,
    string ClinicName,
    string? ClinicCode,
    bool IsPrimary,
    DateOnly? StartDate,
    DateOnly? EndDate);

public sealed record DoctorDto(
    Guid Id,
    Guid? UserId,
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    string LicenseNumber,
    string LicenseCountry,
    string? Biography,
    string? PhotoUrl,
    Gender Gender,
    DateOnly? DateOfBirth,
    DoctorStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<DoctorSpecialtyDto> Specialties,
    IReadOnlyList<DoctorClinicDto> Clinics,
    IReadOnlyList<string> Languages);

public sealed record DoctorClinicAssignmentDto(
    Guid ClinicId,
    bool IsPrimary,
    DateOnly? StartDate,
    DateOnly? EndDate);

public sealed record CreateDoctorCommand(
    Guid? UserId,
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    string LicenseNumber,
    string LicenseCountry,
    string? Biography,
    string? PhotoUrl,
    Gender Gender,
    DateOnly? DateOfBirth,
    DoctorStatus Status,
    IReadOnlyList<Guid> SpecialtyIds,
    IReadOnlyList<DoctorClinicAssignmentDto> Clinics,
    IReadOnlyList<string> Languages);

public sealed record UpdateDoctorCommand(
    Guid Id,
    Guid? UserId,
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    string LicenseNumber,
    string LicenseCountry,
    string? Biography,
    string? PhotoUrl,
    Gender Gender,
    DateOnly? DateOfBirth,
    DoctorStatus Status,
    IReadOnlyList<Guid> SpecialtyIds,
    IReadOnlyList<DoctorClinicAssignmentDto> Clinics,
    IReadOnlyList<string> Languages);

public sealed record DoctorQuery(
    string? Search,
    Guid? SpecialtyId,
    Guid? ClinicId,
    DoctorStatus? Status,
    int Page,
    int PageSize);