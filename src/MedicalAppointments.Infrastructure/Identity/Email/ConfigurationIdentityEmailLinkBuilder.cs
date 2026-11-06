using MedicalAppointments.Application.Auth;
using Microsoft.Extensions.Options;

namespace MedicalAppointments.Infrastructure.Identity.Email;

public sealed class ConfigurationIdentityEmailLinkBuilder : IIdentityEmailLinkBuilder
{
    private readonly AuthOptions _options;

    public ConfigurationIdentityEmailLinkBuilder(IOptions<AuthOptions> optionsAccessor)
    {
        _options = optionsAccessor.Value;
    }

    public string BuildEmailConfirmationLink(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        var baseUrl = _options.Email.BaseUrl.TrimEnd('/');

        return $"{baseUrl}/confirm-email?token={Uri.EscapeDataString(token)}";
    }

    public string BuildPasswordResetLink(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        var baseUrl = _options.Email.BaseUrl.TrimEnd('/');

        return $"{baseUrl}/reset-password?token={Uri.EscapeDataString(token)}";
    }
}