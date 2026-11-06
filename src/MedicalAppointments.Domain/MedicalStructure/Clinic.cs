using MedicalAppointments.Domain.Common;

namespace MedicalAppointments.Domain.MedicalStructure;

public sealed class Clinic : AggregateRoot
{
    private Clinic()
    {
    }

    public Guid OrganizationId { get; private set; }

    public Organization Organization { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string NormalizedName { get; private set; } = null!;

    public string? Code { get; private set; }

    public string? NormalizedCode { get; private set; }

    public string? Description { get; private set; }

    public string? Address { get; private set; }

    public string? City { get; private set; }

    public string? Country { get; private set; }

    public string? Phone { get; private set; }

    public string? Email { get; private set; }

    public string? TimeZoneId { get; private set; }

    public bool IsActive { get; private set; } = true;

    public static Clinic Create(
        Guid organizationId,
        string name,
        string? code,
        string? description,
        string? address,
        string? city,
        string? country,
        string? phone,
        string? email,
        string? timeZoneId,
        bool isActive,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (organizationId == Guid.Empty)
        {
            throw new DomainException("Organization is required.");
        }

        return new Clinic
        {
            Id = Guid.CreateVersion7(),
            OrganizationId = organizationId,
            Name = name.Trim(),
            NormalizedName = StringNormalizer.NormalizeUpper(name),
            Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim(),
            NormalizedCode = StringNormalizer.NormalizeUpperOrNull(code),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim(),
            City = string.IsNullOrWhiteSpace(city) ? null : city.Trim(),
            Country = string.IsNullOrWhiteSpace(country) ? null : country.Trim(),
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            TimeZoneId = string.IsNullOrWhiteSpace(timeZoneId) ? null : timeZoneId.Trim(),
            IsActive = isActive,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(
        string name,
        string? code,
        string? description,
        string? address,
        string? city,
        string? country,
        string? phone,
        string? email,
        string? timeZoneId,
        bool isActive,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name.Trim();
        NormalizedName = StringNormalizer.NormalizeUpper(name);
        Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim();
        NormalizedCode = StringNormalizer.NormalizeUpperOrNull(code);
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();
        City = string.IsNullOrWhiteSpace(city) ? null : city.Trim();
        Country = string.IsNullOrWhiteSpace(country) ? null : country.Trim();
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        TimeZoneId = string.IsNullOrWhiteSpace(timeZoneId) ? null : timeZoneId.Trim();
        IsActive = isActive;

        Touch(now);
    }

    public void SetActive(bool isActive, DateTimeOffset now)
    {
        IsActive = isActive;
        Touch(now);
    }
}