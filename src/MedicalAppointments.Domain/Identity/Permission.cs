namespace MedicalAppointments.Domain.Identity;

public sealed class Permission
{
    private Permission()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public string NormalizedName { get; private set; } = null!;

    public string? Description { get; private set; }

    public string? Group { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Permission Create(
        string name,
        string normalizedName,
        string? description,
        string? group,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedName);

        return new Permission
        {
            Id = Guid.CreateVersion7(),
            Name = name.Trim(),
            NormalizedName = normalizedName,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Group = string.IsNullOrWhiteSpace(group) ? null : group.Trim(),
            CreatedAt = now
        };
    }
}