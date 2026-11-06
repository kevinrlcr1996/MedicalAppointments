using MedicalAppointments.Domain.Common;

namespace MedicalAppointments.Domain.MedicalStructure;

public sealed class Room : AggregateRoot
{
    private Room()
    {
    }

    public Guid ClinicId { get; private set; }

    public Clinic Clinic { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string NormalizedName { get; private set; } = null!;

    public string? Code { get; private set; }

    public string? NormalizedCode { get; private set; }

    public int? Capacity { get; private set; }

    public string? Notes { get; private set; }

    public bool IsActive { get; private set; } = true;

    public static Room Create(
        Guid clinicId,
        string name,
        string? code,
        int? capacity,
        string? notes,
        bool isActive,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (clinicId == Guid.Empty)
        {
            throw new DomainException("Clinic is required.");
        }

        if (capacity is < 1)
        {
            throw new DomainException("Capacity must be greater than zero.");
        }

        return new Room
        {
            Id = Guid.CreateVersion7(),
            ClinicId = clinicId,
            Name = name.Trim(),
            NormalizedName = StringNormalizer.NormalizeUpper(name),
            Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim(),
            NormalizedCode = StringNormalizer.NormalizeUpperOrNull(code),
            Capacity = capacity,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            IsActive = isActive,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(
        string name,
        string? code,
        int? capacity,
        string? notes,
        bool isActive,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (capacity is < 1)
        {
            throw new DomainException("Capacity must be greater than zero.");
        }

        Name = name.Trim();
        NormalizedName = StringNormalizer.NormalizeUpper(name);
        Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim();
        NormalizedCode = StringNormalizer.NormalizeUpperOrNull(code);
        Capacity = capacity;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        IsActive = isActive;

        Touch(now);
    }

    public void SetActive(bool isActive, DateTimeOffset now)
    {
        IsActive = isActive;
        Touch(now);
    }
}