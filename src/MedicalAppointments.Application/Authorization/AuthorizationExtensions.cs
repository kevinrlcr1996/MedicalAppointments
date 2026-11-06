using System.Security.Claims;

namespace MedicalAppointments.Application.Authorization;

public static class AuthorizationExtensions
{
    public static bool HasPermission(this ClaimsPrincipal principal, string permission)
    {
        ArgumentNullException.ThrowIfNull(principal);

        if (principal.IsInRole(Roles.SuperAdmin))
        {
            return true;
        }

        return principal.HasClaim(AppClaimTypes.Permission, permission);
    }
}