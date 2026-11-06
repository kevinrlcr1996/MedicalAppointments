using MedicalAppointments.Domain.Common;

namespace MedicalAppointments.Domain.MedicalStructure;

public sealed class Specialty : AggregateRoot
{
    private Specialty()
    {
    }

    public string Name { get; private set; } = null!;

    public string NormalizedName { get; private set; } = null!;

    public string? Code { get; private set; }

    public string? NormalizedCode { get; private set; }

    public string? Description { get; private set; }

    public bool IsActive { get; private set; } = true;

    public static Specialty Create(
        string name,
        string? code,
        string? description,
        bool isActive,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Specialty
        {
            Id = Guid.CreateVersion7(),
            Name = name.Trim(),
            NormalizedName = StringNormalizer.NormalizeUpper(name),
            Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim(),
            NormalizedCode = StringNormalizer.NormalizeUpperOrNull(code),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            IsActive = isActive,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(
        string name,
        string? code,
        string? description,
        bool isActive,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name.Trim();
        NormalizedName = StringNormalizer.NormalizeUpper(name);
        Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim();
        NormalizedCode = StringNormalizer.NormalizeUpperOrNull(code);
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