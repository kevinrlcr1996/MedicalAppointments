namespace MedicalAppointments.Application.Auth;

public sealed record IssuedAccessToken(
    string Token,
    DateTimeOffset ExpiresAt);