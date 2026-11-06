using MedicalAppointments.Domain.Common;

namespace MedicalAppointments.Domain.MedicalStructure;

public sealed class Patient : AggregateRoot
{
    private Patient()
    {
    }

    public Guid? UserId { get; private set; }

    public string FirstName { get; private set; } = null!;

    public string LastName { get; private set; } = null!;

    public string NormalizedFullName { get; private set; } = null!;

    public DateOnly? DateOfBirth { get; private set; }

    public Gender Gender { get; private set; }

    public string? IdentificationNumber { get; private set; }

    public string? NormalizedIdentificationNumber { get; private set; }

    public string? Phone { get; private set; }

    public string? Email { get; private set; }

    public string? Address { get; private set; }

    public string? EmergencyContact { get; private set; }

    public string? EmergencyPhone { get; private set; }

    public BloodType BloodType { get; private set; }

    public string? InsuranceProvider { get; private set; }

    public string? InsuranceNumber { get; private set; }

    public static Patient Create(
        Guid? userId,
        string firstName,
        string lastName,
        DateOnly? dateOfBirth,
        Gender gender,
        string? identificationNumber,
        string? phone,
        string? email,
        string? address,
        string? emergencyContact,
        string? emergencyPhone,
        BloodType bloodType,
        string? insuranceProvider,
        string? insuranceNumber,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);

        var patient = new Patient
        {
            Id = Guid.CreateVersion7(),
            CreatedAt = now,
            UpdatedAt = now
        };

        patient.SetProfile(
            userId,
            firstName,
            lastName,
            dateOfBirth,
            gender,
            identificationNumber,
            phone,
            email,
            address,
            emergencyContact,
            emergencyPhone,
            bloodType,
            insuranceProvider,
            insuranceNumber,
            now);

        return patient;
    }

    public void UpdateProfile(
        Guid? userId,
        string firstName,
        string lastName,
        DateOnly? dateOfBirth,
        Gender gender,
        string? identificationNumber,
        string? phone,
        string? email,
        string? address,
        string? emergencyContact,
        string? emergencyPhone,
        BloodType bloodType,
        string? insuranceProvider,
        string? insuranceNumber,
        DateTimeOffset now)
    {
        SetProfile(
            userId,
            firstName,
            lastName,
            dateOfBirth,
            gender,
            identificationNumber,
            phone,
            email,
            address,
            emergencyContact,
            emergencyPhone,
            bloodType,
            insuranceProvider,
            insuranceNumber,
            now);
    }

    private void SetProfile(
        Guid? userId,
        string firstName,
        string lastName,
        DateOnly? dateOfBirth,
        Gender gender,
        string? identificationNumber,
        string? phone,
        string? email,
        string? address,
        string? emergencyContact,
        string? emergencyPhone,
        BloodType bloodType,
        string? insuranceProvider,
        string? insuranceNumber,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);

        UserId = userId;
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        NormalizedFullName = StringNormalizer.NormalizeUpper($"{FirstName} {LastName}");
        DateOfBirth = dateOfBirth;
        Gender = gender;
        IdentificationNumber = string.IsNullOrWhiteSpace(identificationNumber) ? null : identificationNumber.Trim();
        NormalizedIdentificationNumber = StringNormalizer.NormalizeUpperOrNull(IdentificationNumber);
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();
        EmergencyContact = string.IsNullOrWhiteSpace(emergencyContact) ? null : emergencyContact.Trim();
        EmergencyPhone = string.IsNullOrWhiteSpace(emergencyPhone) ? null : emergencyPhone.Trim();
        BloodType = bloodType;
        InsuranceProvider = string.IsNullOrWhiteSpace(insuranceProvider) ? null : insuranceProvider.Trim();
        InsuranceNumber = string.IsNullOrWhiteSpace(insuranceNumber) ? null : insuranceNumber.Trim();

        Touch(now);
    }
}