using MedicalAppointments.Application.Common;
using Microsoft.AspNetCore.Http;

namespace MedicalAppointments.Infrastructure.Services;

public sealed class CorrelationIdProvider : ICorrelationIdProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? CorrelationId
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;

            if (context is null)
            {
                return null;
            }

            if (context.Items.TryGetValue("CorrelationId", out var value) && value is string correlationId)
            {
                return correlationId;
            }

            return context.TraceIdentifier;
        }
    }
}