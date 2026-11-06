namespace MedicalAppointments.Application.Auth;

public interface IAuthService
{
    Task<RegistrationResult> RegisterAsync(
        RegisterCommand command,
        CancellationToken cancellationToken = default);

    Task<AuthenticationResult> LoginAsync(
        LoginCommand command,
        RequestContext requestContext,
        CancellationToken cancellationToken = default);

    Task<AuthenticationResult> RefreshAsync(
        string refreshToken,
        RequestContext requestContext,
        CancellationToken cancellationToken = default);

    Task LogoutAsync(
        string? refreshToken,
        RequestContext requestContext,
        CancellationToken cancellationToken = default);

    Task<SimpleResult> ForgotPasswordAsync(
        ForgotPasswordCommand command,
        CancellationToken cancellationToken = default);

    Task<SimpleResult> ResetPasswordAsync(
        ResetPasswordCommand command,
        RequestContext requestContext,
        CancellationToken cancellationToken = default);

    Task<SimpleResult> ConfirmEmailAsync(
        ConfirmEmailCommand command,
        CancellationToken cancellationToken = default);

    Task<UserProfile> GetCurrentUserAsync(
        CancellationToken cancellationToken = default);
}