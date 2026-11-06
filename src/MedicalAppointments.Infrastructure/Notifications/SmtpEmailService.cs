using MailKit.Security;
using MedicalAppointments.Application.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace MedicalAppointments.Infrastructure.Notifications;

public sealed class SmtpEmailService : IEmailService
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
        IOptions<SmtpOptions> optionsAccessor,
        ILogger<SmtpEmailService> logger)
    {
        _options = optionsAccessor.Value;
        _logger = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var mimeMessage = new MimeMessage();

        mimeMessage.From.Add(new MailboxAddress(_options.FromName, _options.From));
        mimeMessage.To.Add(new MailboxAddress(string.Empty, message.To));
        mimeMessage.Subject = message.Subject;

        mimeMessage.Body = new TextPart("html")
        {
            Text = message.HtmlBody
        };

        using var client = new MailKit.Net.Smtp.SmtpClient();

        var socketOptions = _options.UseSsl
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.None;

        await client.ConnectAsync(_options.Host, _options.Port, socketOptions, cancellationToken);

        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
        }

        await client.SendAsync(mimeMessage, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        _logger.LogInformation("Email sent to {To}.", message.To);
    }
}