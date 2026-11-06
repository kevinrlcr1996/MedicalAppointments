using MedicalAppointments.Application.Common;
using Microsoft.AspNetCore.Http;

namespace MedicalAppointments.Infrastructure.Services;

public sealed class RequestMetadataProvider : IRequestMetadataProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RequestMetadataProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? IpAddress =>
        _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent =>
        _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();
}