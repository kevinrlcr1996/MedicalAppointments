namespace MedicalAppointments.Application.Auth;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string? FirstName,
    string? LastName);

public sealed record LoginCommand(
    string Email,
    string Password);

public sealed record ForgotPasswordCommand(
    string Email);

public sealed record ResetPasswordCommand(
    string Token,
    string Password);

public sealed record ConfirmEmailCommand(
    string Token);