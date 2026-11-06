namespace MedicalAppointments.Domain.Identity;

public sealed class UserPermission
{
    private UserPermission()
    {
    }

    public UserPermission(Guid userId, Guid permissionId, DateTimeOffset grantedAt)
    {
        UserId = userId;
        PermissionId = permissionId;
        GrantedAt = grantedAt;
    }

    public Guid UserId { get; private set; }

    public Guid PermissionId { get; private set; }

    public DateTimeOffset GrantedAt { get; private set; }

    public User User { get; private set; } = null!;

    public Permission Permission { get; private set; } = null!;
}