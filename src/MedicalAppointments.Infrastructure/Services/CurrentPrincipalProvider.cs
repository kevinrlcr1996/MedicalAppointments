using System.Security.Claims;
using MedicalAppointments.Application.Common;
using Microsoft.AspNetCore.Http;

namespace MedicalAppointments.Infrastructure.Services;

public sealed class CurrentPrincipalProvider : ICurrentPrincipalProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentPrincipalProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public ClaimsPrincipal? GetCurrentPrincipal()
    {
        return _httpContextAccessor.HttpContext?.User;
    }
}