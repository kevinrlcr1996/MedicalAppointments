namespace MedicalAppointments.Infrastructure.Notifications;

public sealed class SmtpOptions
{
    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 1025;

    public string? Username { get; set; }

    public string? Password { get; set; }

    public string From { get; set; } = "no-reply@localhost";

    public string FromName { get; set; } = "Medical Appointments";

    public bool UseSsl { get; set; }
}