using MedicalAppointments.Domain.Common;

namespace MedicalAppointments.Domain.MedicalStructure;

public sealed class Organization : AggregateRoot
{
    private Organization()
    {
    }

    public string Name { get; private set; } = null!;

    public string NormalizedName { get; private set; } = null!;

    public string? LegalName { get; private set; }

    public string? TaxId { get; private set; }

    public string? NormalizedTaxId { get; private set; }

    public string? Email { get; private set; }

    public string? Phone { get; private set; }

    public string? Address { get; private set; }

    public string? City { get; private set; }

    public string? Country { get; private set; }

    public bool IsActive { get; private set; } = true;

    public static Organization Create(
        string name,
        string? legalName,
        string? taxId,
        string? email,
        string? phone,
        string? address,
        string? city,
        string? country,
        bool isActive,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Organization
        {
            Id = Guid.CreateVersion7(),
            Name = name.Trim(),
            NormalizedName = StringNormalizer.NormalizeUpper(name),
            LegalName = string.IsNullOrWhiteSpace(legalName) ? null : legalName.Trim(),
            TaxId = string.IsNullOrWhiteSpace(taxId) ? null : taxId.Trim(),
            NormalizedTaxId = StringNormalizer.NormalizeUpperOrNull(taxId),
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim(),
            City = string.IsNullOrWhiteSpace(city) ? null : city.Trim(),
            Country = string.IsNullOrWhiteSpace(country) ? null : country.Trim(),
            IsActive = isActive,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(
        string name,
        string? legalName,
        string? taxId,
        string? email,
        string? phone,
        string? address,
        string? city,
        string? country,
        bool isActive,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name.Trim();
        NormalizedName = StringNormalizer.NormalizeUpper(name);
        LegalName = string.IsNullOrWhiteSpace(legalName) ? null : legalName.Trim();
        TaxId = string.IsNullOrWhiteSpace(taxId) ? null : taxId.Trim();
        NormalizedTaxId = StringNormalizer.NormalizeUpperOrNull(taxId);
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        Address = string.IsNullOrWhiteSpace(address) ? null : address.Trim();
        City = string.IsNullOrWhiteSpace(city) ? null : city.Trim();
        Country = string.IsNullOrWhiteSpace(country) ? null : country.Trim();
        IsActive = isActive;

        Touch(now);
    }

    public void SetActive(bool isActive, DateTimeOffset now)
    {
        IsActive = isActive;
        Touch(now);
    }
}