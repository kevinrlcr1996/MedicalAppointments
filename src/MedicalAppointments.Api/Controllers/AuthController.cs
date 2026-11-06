using Asp.Versioning;
using MedicalAppointments.Application.Auth;
using MedicalAppointments.Contracts.Auth;
using MedicalAppointments.Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace MedicalAppointments.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/auth")]
[ApiVersion("1.0")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly AuthOptions _options;

    public AuthController(
        IAuthService authService,
        IOptions<AuthOptions> optionsAccessor)
    {
        _authService = authService;
        _options = optionsAccessor.Value;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-register")]
    [ProducesResponseType(typeof(RegistrationResponse), StatusCodes.Status202Accepted)]
    public async Task<ActionResult<RegistrationResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterCommand(
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName);

        var result = await _authService.RegisterAsync(command, cancellationToken);

        return Accepted(new RegistrationResponse(result.Accepted, result.Message));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-login")]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthenticationResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var command = new LoginCommand(request.Email, request.Password);

        var result = await _authService.LoginAsync(command, HttpContext.ToRequestContext(), cancellationToken);

        HttpContext.SetRefreshTokenCookie(result.RefreshToken, _options.RefreshToken);

        return Ok(MapAuthentication(result));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-refresh")]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthenticationResponse>> Refresh(
        CancellationToken cancellationToken)
    {
        var refreshToken = HttpContext.GetRefreshTokenCookie(_options.RefreshToken);

        var result = await _authService.RefreshAsync(
            refreshToken ?? string.Empty,
            HttpContext.ToRequestContext(),
            cancellationToken);

        HttpContext.SetRefreshTokenCookie(result.RefreshToken, _options.RefreshToken);

        return Ok(MapAuthentication(result));
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SimpleResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SimpleResponse>> Logout(
        CancellationToken cancellationToken)
    {
        var refreshToken = HttpContext.GetRefreshTokenCookie(_options.RefreshToken);

        await _authService.LogoutAsync(
            refreshToken,
            HttpContext.ToRequestContext(),
            cancellationToken);

        HttpContext.DeleteRefreshTokenCookie(_options.RefreshToken);

        return Ok(new SimpleResponse(true, "Logged out successfully."));
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-forgot")]
    [ProducesResponseType(typeof(SimpleResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SimpleResponse>> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ForgotPasswordCommand(request.Email);

        var result = await _authService.ForgotPasswordAsync(command, cancellationToken);

        return Ok(new SimpleResponse(result.Success, result.Message));
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-reset")]
    [ProducesResponseType(typeof(SimpleResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SimpleResponse>> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ResetPasswordCommand(request.Token, request.Password);

        var result = await _authService.ResetPasswordAsync(
            command,
            HttpContext.ToRequestContext(),
            cancellationToken);

        HttpContext.DeleteRefreshTokenCookie(_options.RefreshToken);

        return Ok(new SimpleResponse(result.Success, result.Message));
    }

    [HttpPost("confirm-email")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-confirm")]
    [ProducesResponseType(typeof(SimpleResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<SimpleResponse>> ConfirmEmail(
        [FromBody] ConfirmEmailRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ConfirmEmailCommand(request.Token);

        var result = await _authService.ConfirmEmailAsync(command, cancellationToken);

        return Ok(new SimpleResponse(result.Success, result.Message));
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CurrentUserResponse>> Me(CancellationToken cancellationToken)
    {
        var profile = await _authService.GetCurrentUserAsync(cancellationToken);

        return Ok(MapProfile(profile));
    }

    private static AuthenticationResponse MapAuthentication(AuthenticationResult result)
    {
        return new AuthenticationResponse(
            result.AccessToken,
            result.ExpiresAt,
            MapProfile(result.User));
    }

    private static CurrentUserResponse MapProfile(UserProfile profile)
    {
        return new CurrentUserResponse(
            profile.Id,
            profile.Email,
            profile.FirstName,
            profile.LastName,
            profile.IsEmailConfirmed,
            profile.Roles,
            profile.Permissions);
    }
}