using MedicalAppointments.Application.Notifications;
using Microsoft.Extensions.Logging;

namespace MedicalAppointments.Infrastructure.Notifications;

public sealed class DevelopmentEmailService : IEmailService
{
    private readonly ILogger<DevelopmentEmailService> _logger;

    public DevelopmentEmailService(ILogger<DevelopmentEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Development email simulated. To: {To}, Subject: {Subject}",
            message.To,
            message.Subject);

        return Task.CompletedTask;
    }
}