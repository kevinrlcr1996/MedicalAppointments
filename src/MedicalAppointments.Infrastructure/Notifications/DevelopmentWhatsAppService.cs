using MedicalAppointments.Application.Notifications;
using Microsoft.Extensions.Logging;

namespace MedicalAppointments.Infrastructure.Notifications;

public sealed class DevelopmentWhatsAppService : IWhatsAppService
{
    private readonly ILogger<DevelopmentWhatsAppService> _logger;

    public DevelopmentWhatsAppService(ILogger<DevelopmentWhatsAppService> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(WhatsAppMessage message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Development WhatsApp simulated. To: {To}, Body: {Body}",
            message.To,
            message.Body);

        return Task.CompletedTask;
    }
}