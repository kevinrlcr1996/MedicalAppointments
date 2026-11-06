namespace MedicalAppointments.Contracts.Auth;

public sealed record RegisterRequest(
    string Email,
    string Password,
    string? FirstName,
    string? LastName);

public sealed record LoginRequest(
    string Email,
    string Password);

public sealed record ForgotPasswordRequest(
    string Email);

public sealed record ResetPasswordRequest(
    string Token,
    string Password);

public sealed record ConfirmEmailRequest(
    string Token);

public sealed record CurrentUserResponse(
    Guid Id,
    string Email,
    string? FirstName,
    string? LastName,
    bool IsEmailConfirmed,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

public sealed record AuthenticationResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    CurrentUserResponse User);

public sealed record RegistrationResponse(
    bool Accepted,
    string Message);

public sealed record SimpleResponse(
    bool Success,
    string Message);