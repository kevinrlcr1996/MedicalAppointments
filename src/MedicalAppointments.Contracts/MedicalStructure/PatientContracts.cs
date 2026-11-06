using MedicalAppointments.Domain.MedicalStructure;

namespace MedicalAppointments.Contracts.MedicalStructure;

public sealed record PatientResponse(
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

public sealed record CreatePatientRequest(
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

public sealed record UpdatePatientRequest(
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

// Self-service: sin UserId. El backend fuerza el del token.
public sealed record CreateSelfPatientRequest(
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

public sealed record UpdateSelfPatientRequest(
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