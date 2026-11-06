using System.Security.Claims;
using MedicalAppointments.Application.Authorization;
using MedicalAppointments.Application.Common;
using Microsoft.AspNetCore.Http;

namespace MedicalAppointments.Infrastructure.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? UserName => _httpContextAccessor.HttpContext?.User.Identity?.Name;

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public IReadOnlyCollection<string> Permissions
    {
        get
        {
            var claims = _httpContextAccessor.HttpContext?.User.FindAll(AppClaimTypes.Permission);

            if (claims is null)
            {
                return Array.Empty<string>();
            }

            return claims.Select(x => x.Value).Distinct(StringComparer.Ordinal).ToArray();
        }
    }
}