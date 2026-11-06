namespace MedicalAppointments.Application.Features;

public interface IFeatureFlagService
{
    bool IsEnabled(string featureName);
}