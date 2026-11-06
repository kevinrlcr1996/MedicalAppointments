using MedicalAppointments.Domain.Common;

namespace MedicalAppointments.Domain.MedicalStructure;

public sealed record DoctorClinicAssignment(
    Guid ClinicId,
    bool IsPrimary,
    DateOnly? StartDate,
    DateOnly? EndDate);

public sealed class Doctor : AggregateRoot
{
    private Doctor()
    {
    }

    public Guid? UserId { get; private set; }

    public string FirstName { get; private set; } = null!;

    public string LastName { get; private set; } = null!;

    public string NormalizedFullName { get; private set; } = null!;

    public string? Email { get; private set; }

    public string? Phone { get; private set; }

    public string LicenseNumber { get; private set; } = null!;

    public string NormalizedLicenseNumber { get; private set; } = null!;

    public string LicenseCountry { get; private set; } = null!;

    public string NormalizedLicenseCountry { get; private set; } = null!;

    public string? Biography { get; private set; }

    public string? PhotoUrl { get; private set; }

    public Gender Gender { get; private set; }

    public DateOnly? DateOfBirth { get; private set; }

    public DoctorStatus Status { get; private set; }

    public ICollection<DoctorSpecialty> Specialties { get; private set; } = new List<DoctorSpecialty>();

    public ICollection<DoctorClinic> Clinics { get; private set; } = new List<DoctorClinic>();

    public ICollection<DoctorLanguage> Languages { get; private set; } = new List<DoctorLanguage>();

    public static Doctor Create(
        Guid? userId,
        string firstName,
        string lastName,
        string? email,
        string? phone,
        string licenseNumber,
        string licenseCountry,
        string? biography,
        string? photoUrl,
        Gender gender,
        DateOnly? dateOfBirth,
        DoctorStatus status,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(licenseNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(licenseCountry);

        var doctor = new Doctor
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            CreatedAt = now,
            UpdatedAt = now
        };

        doctor.SetProfile(
            firstName,
            lastName,
            email,
            phone,
            licenseNumber,
            licenseCountry,
            biography,
            photoUrl,
            gender,
            dateOfBirth,
            status,
            now);

        return doctor;
    }

    public void UpdateProfile(
        Guid? userId,
        string firstName,
        string lastName,
        string? email,
        string? phone,
        string licenseNumber,
        string licenseCountry,
        string? biography,
        string? photoUrl,
        Gender gender,
        DateOnly? dateOfBirth,
        DoctorStatus status,
        DateTimeOffset now)
    {
        UserId = userId;

        SetProfile(
            firstName,
            lastName,
            email,
            phone,
            licenseNumber,
            licenseCountry,
            biography,
            photoUrl,
            gender,
            dateOfBirth,
            status,
            now);
    }

    public void SetStatus(DoctorStatus status, DateTimeOffset now)
    {
        Status = status;
        Touch(now);
    }

    public void ReplaceSpecialties(IEnumerable<Guid> specialtyIds, DateTimeOffset now)
    {
        Specialties.Clear();

        foreach (var specialtyId in specialtyIds.Distinct())
        {
            if (specialtyId == Guid.Empty)
            {
                throw new DomainException("Specialty id cannot be empty.");
            }

            Specialties.Add(new DoctorSpecialty(Id, specialtyId, now));
        }

        Touch(now);
    }

    public void ReplaceClinics(IEnumerable<DoctorClinicAssignment> assignments, DateTimeOffset now)
    {
        var list = assignments.ToList();

        if (list.Count(x => x.IsPrimary) > 1)
        {
            throw new DomainException("A doctor can have only one primary clinic.");
        }

        Clinics.Clear();

        foreach (var assignment in list)
        {
            if (assignment.ClinicId == Guid.Empty)
            {
                throw new DomainException("Clinic id cannot be empty.");
            }

            if (assignment.StartDate.HasValue &&
                assignment.EndDate.HasValue &&
                assignment.EndDate.Value < assignment.StartDate.Value)
            {
                throw new DomainException("Clinic end date cannot be earlier than start date.");
            }

            Clinics.Add(new DoctorClinic(
                Id,
                assignment.ClinicId,
                assignment.IsPrimary,
                assignment.StartDate,
                assignment.EndDate,
                now));
        }

        Touch(now);
    }

    public void ReplaceLanguages(IEnumerable<string> languages, DateTimeOffset now)
    {
        Languages.Clear();

        var normalized = languages
            .Select(StringNormalizer.NormalizeUpper)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var language in normalized)
        {
            Languages.Add(new DoctorLanguage(Id, language, now));
        }

        Touch(now);
    }

    private void SetProfile(
        string firstName,
        string lastName,
        string? email,
        string? phone,
        string licenseNumber,
        string licenseCountry,
        string? biography,
        string? photoUrl,
        Gender gender,
        DateOnly? dateOfBirth,
        DoctorStatus status,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(licenseNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(licenseCountry);

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        NormalizedFullName = StringNormalizer.NormalizeUpper($"{FirstName} {LastName}");
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        LicenseNumber = licenseNumber.Trim();
        NormalizedLicenseNumber = StringNormalizer.NormalizeUpper(LicenseNumber);
        LicenseCountry = licenseCountry.Trim();
        NormalizedLicenseCountry = StringNormalizer.NormalizeUpper(LicenseCountry);
        Biography = string.IsNullOrWhiteSpace(biography) ? null : biography.Trim();
        PhotoUrl = string.IsNullOrWhiteSpace(photoUrl) ? null : photoUrl.Trim();
        Gender = gender;
        DateOfBirth = dateOfBirth;
        Status = status;

        Touch(now);
    }
}