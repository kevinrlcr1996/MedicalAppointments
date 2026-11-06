namespace MedicalAppointments.Domain.Identity;

public sealed class Role
{
    private Role()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public string NormalizedName { get; private set; } = null!;

    public string? Description { get; private set; }

    public bool IsSystem { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public ICollection<RolePermission> Permissions { get; private set; } = new List<RolePermission>();

    public static Role Create(
        string name,
        string normalizedName,
        string? description,
        bool isSystem,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedName);

        return new Role
        {
            Id = Guid.CreateVersion7(),
            Name = name.Trim(),
            NormalizedName = normalizedName,
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            IsSystem = isSystem,
            CreatedAt = now
        };
    }

    public void GrantPermission(Guid permissionId, DateTimeOffset now)
    {
        if (Permissions.Any(x => x.PermissionId == permissionId))
        {
            return;
        }

        Permissions.Add(new RolePermission(Id, permissionId, now));
    }
}