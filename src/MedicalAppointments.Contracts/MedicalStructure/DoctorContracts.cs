using MedicalAppointments.Domain.MedicalStructure;

namespace MedicalAppointments.Contracts.MedicalStructure;

public sealed record DoctorSpecialtyResponse(
    Guid SpecialtyId,
    string Name,
    string? Code);

public sealed record DoctorClinicResponse(
    Guid ClinicId,
    string ClinicName,
    string? ClinicCode,
    bool IsPrimary,
    DateOnly? StartDate,
    DateOnly? EndDate);

public sealed record DoctorResponse(
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
    IReadOnlyList<DoctorSpecialtyResponse> Specialties,
    IReadOnlyList<DoctorClinicResponse> Clinics,
    IReadOnlyList<string> Languages);

public sealed record DoctorClinicAssignmentRequest(
    Guid ClinicId,
    bool IsPrimary,
    DateOnly? StartDate,
    DateOnly? EndDate);

public sealed record CreateDoctorRequest(
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
    IReadOnlyList<DoctorClinicAssignmentRequest> Clinics,
    IReadOnlyList<string> Languages);

public sealed record UpdateDoctorRequest(
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
    IReadOnlyList<DoctorClinicAssignmentRequest> Clinics,
    IReadOnlyList<string> Languages);