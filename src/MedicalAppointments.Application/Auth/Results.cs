namespace MedicalAppointments.Application.Auth;

public sealed record AuthenticationResult(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    string RefreshToken,
    UserProfile User);

public sealed record RegistrationResult(
    bool Accepted,
    string Message);

public sealed record SimpleResult(
    bool Success,
    string Message);