namespace MedicalAppointments.Contracts.Auth;

public sealed record DevTokenResponse(
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAt);