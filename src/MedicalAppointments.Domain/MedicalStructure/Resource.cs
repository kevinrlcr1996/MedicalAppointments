using MedicalAppointments.Domain.Common;

namespace MedicalAppointments.Domain.MedicalStructure;

public sealed class Resource : AggregateRoot
{
    private Resource()
    {
    }

    public Guid ClinicId { get; private set; }

    public Clinic Clinic { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string NormalizedName { get; private set; } = null!;

    public string? Code { get; private set; }

    public string? NormalizedCode { get; private set; }

    public ResourceKind Kind { get; private set; }

    public string? Description { get; private set; }

    public bool IsActive { get; private set; } = true;

    public static Resource Create(
        Guid clinicId,
        string name,
        string? code,
        ResourceKind kind,
        string? description,
        bool isActive,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (clinicId == Guid.Empty)
        {
            throw new DomainException("Clinic is required.");
        }

        return new Resource
        {
            Id = Guid.CreateVersion7(),
            ClinicId = clinicId,
            Name = name.Trim(),
            NormalizedName = StringNormalizer.NormalizeUpper(name),
            Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim(),
            NormalizedCode = StringNormalizer.NormalizeUpperOrNull(code),
            Kind = kind,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            IsActive = isActive,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(
        string name,
        string? code,
        ResourceKind kind,
        string? description,
        bool isActive,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name.Trim();
        NormalizedName = StringNormalizer.NormalizeUpper(name);
        Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim();
        NormalizedCode = StringNormalizer.NormalizeUpperOrNull(code);
        Kind = kind;
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        IsActive = isActive;

        Touch(now);
    }

    public void SetActive(bool isActive, DateTimeOffset now)
    {
        IsActive = isActive;
        Touch(now);
    }
}