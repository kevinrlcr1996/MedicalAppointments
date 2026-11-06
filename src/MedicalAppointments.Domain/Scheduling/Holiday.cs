using MedicalAppointments.Domain.Common;

namespace MedicalAppointments.Domain.Scheduling;

public sealed class Holiday : AggregateRoot
{
    private Holiday()
    {
    }

    public Guid? OrganizationId { get; private set; }

    public Guid? ClinicId { get; private set; }

    public DateOnly Date { get; private set; }

    public string Name { get; private set; } = null!;

    public string? Description { get; private set; }

    public bool IsActive { get; private set; } = true;

    public static Holiday Create(
        Guid? organizationId,
        Guid? clinicId,
        DateOnly date,
        string name,
        string? description,
        bool isActive,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Holiday
        {
            Id = Guid.CreateVersion7(),
            OrganizationId = organizationId,
            ClinicId = clinicId,
            Date = date,
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            IsActive = isActive,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(
        Guid? organizationId,
        Guid? clinicId,
        DateOnly date,
        string name,
        string? description,
        bool isActive,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        OrganizationId = organizationId;
        ClinicId = clinicId;
        Date = date;
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        IsActive = isActive;

        Touch(now);
    }

    public void SetActive(bool isActive, DateTimeOffset now)
    {
        IsActive = isActive;
        Touch(now);
    }

    public bool AppliesTo(Guid organizationId, Guid clinicId)
    {
        if (!IsActive)
        {
            return false;
        }

        if (ClinicId == clinicId)
        {
            return true;
        }

        if (OrganizationId == organizationId && ClinicId is null)
        {
            return true;
        }

        return OrganizationId is null && ClinicId is null;
    }
}