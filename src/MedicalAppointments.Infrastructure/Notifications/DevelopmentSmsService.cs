using MedicalAppointments.Application.Notifications;
using Microsoft.Extensions.Logging;

namespace MedicalAppointments.Infrastructure.Notifications;

public sealed class DevelopmentSmsService : ISmsService
{
    private readonly ILogger<DevelopmentSmsService> _logger;

    public DevelopmentSmsService(ILogger<DevelopmentSmsService> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(SmsMessage message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Development SMS simulated. To: {To}, Body: {Body}",
            message.To,
            message.Body);

        return Task.CompletedTask;
    }
}