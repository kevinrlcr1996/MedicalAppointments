using MedicalAppointments.Application.Notifications;
using Microsoft.Extensions.Logging;

namespace MedicalAppointments.Infrastructure.Notifications;

public sealed class DevelopmentPushNotificationService : IPushNotificationService
{
    private readonly ILogger<DevelopmentPushNotificationService> _logger;

    public DevelopmentPushNotificationService(ILogger<DevelopmentPushNotificationService> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(PushNotificationMessage message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Development push simulated. Device: {DeviceToken}, Title: {Title}",
            message.DeviceToken,
            message.Title);

        return Task.CompletedTask;
    }
}