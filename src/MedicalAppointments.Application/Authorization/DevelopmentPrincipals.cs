namespace MedicalAppointments.Application.Authorization;

public static class DevelopmentPrincipals
{
    public static readonly Guid SuperAdminId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public const string SuperAdminUserName = "Super Admin";

    public static readonly string[] SuperAdminRoles =
    [
        Roles.SuperAdmin
    ];

    public static readonly string[] SuperAdminPermissions =
    [
        Permissions.RealtimeTest,
        Permissions.FoundationTestWrite
    ];
}