using MedicalAppointments.Application.Features;
using Microsoft.Extensions.Configuration;

namespace MedicalAppointments.Infrastructure.Services;

public sealed class ConfigurationFeatureFlagService : IFeatureFlagService
{
    private readonly IConfiguration _configuration;

    public ConfigurationFeatureFlagService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public bool IsEnabled(string featureName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(featureName);

        return _configuration.GetValue<bool>($"FeatureFlags:{featureName}");
    }
}