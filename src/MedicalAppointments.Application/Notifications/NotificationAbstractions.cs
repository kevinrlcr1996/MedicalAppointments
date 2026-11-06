namespace MedicalAppointments.Application.Notifications;

public sealed record EmailMessage(
    string To,
    string Subject,
    string HtmlBody,
    string? TextBody = null);

public interface IEmailService
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

public sealed record SmsMessage(
    string To,
    string Body);

public interface ISmsService
{
    Task SendAsync(SmsMessage message, CancellationToken cancellationToken = default);
}

public sealed record WhatsAppMessage(
    string To,
    string Body);

public interface IWhatsAppService
{
    Task SendAsync(WhatsAppMessage message, CancellationToken cancellationToken = default);
}

public sealed record PushNotificationMessage(
    string DeviceToken,
    string Title,
    string Body);

public interface IPushNotificationService
{
    Task SendAsync(PushNotificationMessage message, CancellationToken cancellationToken = default);
}