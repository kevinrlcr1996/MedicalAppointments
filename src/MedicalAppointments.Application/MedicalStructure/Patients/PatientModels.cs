using MedicalAppointments.Domain.MedicalStructure;

namespace MedicalAppointments.Application.MedicalStructure.Patients;

public sealed record PatientDto(
    Guid Id,
    Guid? UserId,
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth,
    Gender Gender,
    string? IdentificationNumber,
    string? Phone,
    string? Email,
    string? Address,
    string? EmergencyContact,
    string? EmergencyPhone,
    BloodType BloodType,
    string? InsuranceProvider,
    string? InsuranceNumber,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreatePatientCommand(
    Guid? UserId,
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth,
    Gender Gender,
    string? IdentificationNumber,
    string? Phone,
    string? Email,
    string? Address,
    string? EmergencyContact,
    string? EmergencyPhone,
    BloodType BloodType,
    string? InsuranceProvider,
    string? InsuranceNumber);

public sealed record UpdatePatientCommand(
    Guid Id,
    Guid? UserId,
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth,
    Gender Gender,
    string? IdentificationNumber,
    string? Phone,
    string? Email,
    string? Address,
    string? EmergencyContact,
    string? EmergencyPhone,
    BloodType BloodType,
    string? InsuranceProvider,
    string? InsuranceNumber);

// Self-service: sin UserId en el body. El UserId lo fuerza el servicio desde el token.
public sealed record CreateSelfPatientCommand(
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth,
    Gender Gender,
    string? IdentificationNumber,
    string? Phone,
    string? Email,
    string? Address,
    string? EmergencyContact,
    string? EmergencyPhone,
    BloodType BloodType,
    string? InsuranceProvider,
    string? InsuranceNumber);

public sealed record UpdateSelfPatientCommand(
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth,
    Gender Gender,
    string? IdentificationNumber,
    string? Phone,
    string? Email,
    string? Address,
    string? EmergencyContact,
    string? EmergencyPhone,
    BloodType BloodType,
    string? InsuranceProvider,
    string? InsuranceNumber);

public sealed record PatientQuery(
    string? Search,
    int Page,
    int PageSize);