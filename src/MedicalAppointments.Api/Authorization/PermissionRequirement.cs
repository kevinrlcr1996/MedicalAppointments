using Microsoft.AspNetCore.Authorization;

namespace MedicalAppointments.Api.Authorization;

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permission)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);

        Permission = permission;
    }

    public string Permission { get; }
}