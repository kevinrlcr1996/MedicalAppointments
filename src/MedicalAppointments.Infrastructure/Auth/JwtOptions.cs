namespace MedicalAppointments.Infrastructure.Auth;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "MedicalAppointments";

    public string Audience { get; set; } = "MedicalAppointments";

    public string Key { get; set; } = string.Empty;

    public int AccessTokenLifetimeMinutes { get; set; } = 60;
}