namespace MedicalAppointments.Application.Auth;

public sealed class AuthOptions
{
    public PasswordOptions Password { get; set; } = new();

    public JwtOptions Jwt { get; set; } = new();

    public RefreshTokenOptions RefreshToken { get; set; } = new();

    public EmailTokenOptions Email { get; set; } = new();
}

public sealed class PasswordOptions
{
    public int MinLength { get; set; } = 10;

    public bool RequireDigit { get; set; } = true;

    public bool RequireLetter { get; set; } = true;

    public bool RequireSpecialCharacter { get; set; } = true;

    public int MaxFailedAttempts { get; set; } = 5;

    public int LockoutDurationMinutes { get; set; } = 15;

    public TimeSpan LockoutDuration => TimeSpan.FromMinutes(Math.Max(1, LockoutDurationMinutes));
}

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "MedicalAppointments";

    public string Audience { get; set; } = "MedicalAppointments";

    public string Key { get; set; } = string.Empty;

    public int AccessTokenLifetimeMinutes { get; set; } = 15;

    public TimeSpan AccessTokenLifetime => TimeSpan.FromMinutes(Math.Max(1, AccessTokenLifetimeMinutes));
}

public sealed class RefreshTokenOptions
{
    public int SlidingLifetimeDays { get; set; } = 7;

    public int AbsoluteLifetimeDays { get; set; } = 30;

    public string CookieName { get; set; } = "MA_REFRESH_TOKEN";

    public string CookiePath { get; set; } = "/api/v1/auth";

    public bool CookieSecure { get; set; }

    public TimeSpan SlidingLifetime => TimeSpan.FromDays(Math.Max(1, SlidingLifetimeDays));

    public TimeSpan AbsoluteLifetime => TimeSpan.FromDays(Math.Max(SlidingLifetimeDays, AbsoluteLifetimeDays));
}

public sealed class EmailTokenOptions
{
    public string BaseUrl { get; set; } = "http://localhost:5173";

    public string SenderName { get; set; } = "Medical Appointments";

    public int ConfirmationLifetimeHours { get; set; } = 24;

    public int PasswordResetLifetimeMinutes { get; set; } = 30;

    public bool RequireConfirmation { get; set; } = true;

    public TimeSpan ConfirmationLifetime => TimeSpan.FromHours(Math.Max(1, ConfirmationLifetimeHours));

    public TimeSpan PasswordResetLifetime => TimeSpan.FromMinutes(Math.Max(1, PasswordResetLifetimeMinutes));
}