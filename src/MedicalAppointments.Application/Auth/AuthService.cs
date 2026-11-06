using MedicalAppointments.Application.Authorization;
using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.Common.Exceptions;
using MedicalAppointments.Application.Notifications;
using MedicalAppointments.Domain.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MedicalAppointments.Application.Auth;

public sealed class AuthService : IAuthService
{
    private readonly IUserStore _userStore;
    private readonly IRefreshTokenStore _refreshTokenStore;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISecureTokenGenerator _tokenGenerator;
    private readonly ITokenHasher _tokenHasher;
    private readonly IAccessTokenIssuer _accessTokenIssuer;
    private readonly IIdentityEmailLinkBuilder _emailLinkBuilder;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly TimeProvider _timeProvider;
    private readonly AuthOptions _options;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserStore userStore,
        IRefreshTokenStore refreshTokenStore,
        IPasswordHasher passwordHasher,
        ISecureTokenGenerator tokenGenerator,
        ITokenHasher tokenHasher,
        IAccessTokenIssuer accessTokenIssuer,
        IIdentityEmailLinkBuilder emailLinkBuilder,
        IEmailService emailService,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        TimeProvider timeProvider,
        IOptions<AuthOptions> optionsAccessor,
        ILogger<AuthService> logger)
    {
        _userStore = userStore;
        _refreshTokenStore = refreshTokenStore;
        _passwordHasher = passwordHasher;
        _tokenGenerator = tokenGenerator;
        _tokenHasher = tokenHasher;
        _accessTokenIssuer = accessTokenIssuer;
        _emailLinkBuilder = emailLinkBuilder;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
        _options = optionsAccessor.Value;
        _logger = logger;
    }

    public async Task<RegistrationResult> RegisterAsync(
        RegisterCommand command,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var normalizedEmail = EmailNormalizer.Normalize(command.Email);

        if (await _userStore.EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            throw new ConflictException("A user with this email already exists.");
        }

        var passwordHash = _passwordHasher.Hash(command.Password);

        var user = User.Create(
            email: command.Email,
            normalizedEmail: normalizedEmail,
            passwordHash: passwordHash,
            firstName: command.FirstName,
            lastName: command.LastName,
            isEmailConfirmed: !_options.Email.RequireConfirmation,
            now: now);

        string? confirmationToken = null;

        if (_options.Email.RequireConfirmation)
        {
            confirmationToken = _tokenGenerator.Generate();
            var tokenHash = _tokenHasher.Hash(confirmationToken);
            var expiresAt = now + _options.Email.ConfirmationLifetime;

            user.SetEmailConfirmationToken(tokenHash, expiresAt, now);
        }

        await _userStore.AddAsync(user, cancellationToken);
        await _userStore.AssignRoleByNameAsync(user, Roles.Patient, now, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (confirmationToken is not null)
        {
            await TrySendEmailConfirmationAsync(user.Email, confirmationToken, cancellationToken);
        }

        return new RegistrationResult(
            true,
            _options.Email.RequireConfirmation
                ? "If the account was created, a confirmation email has been sent."
                : "Account created successfully.");
    }

    public async Task<AuthenticationResult> LoginAsync(
        LoginCommand command,
        RequestContext requestContext,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var normalizedEmail = EmailNormalizer.Normalize(command.Email);

        var user = await _userStore.FindTrackedByEmailAsync(normalizedEmail, cancellationToken)
            ?? throw new InvalidCredentialsException();

        if (user.IsLockedOut(now))
        {
            throw new AccountLockedException();
        }

        if (_options.Email.RequireConfirmation && !user.IsEmailConfirmed)
        {
            throw new EmailNotConfirmedException();
        }

        if (!_passwordHasher.Verify(command.Password, user.PasswordHash))
        {
            user.RegisterFailedLogin(
                _options.Password.MaxFailedAttempts,
                _options.Password.LockoutDuration,
                now);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            throw new InvalidCredentialsException();
        }

        user.ResetFailedLogin(now);

        var authData = await _userStore.GetAuthenticationDataAsync(user.Id, cancellationToken);

        var accessToken = _accessTokenIssuer.Issue(new AccessTokenRequest(
            UserId: user.Id,
            Email: user.Email,
            DisplayName: BuildDisplayName(user),
            Roles: authData.Roles,
            Permissions: authData.Permissions,
            SecurityStamp: user.SecurityStamp));

        var refreshToken = await CreateRefreshTokenAsync(
            user.Id,
            familyId: Guid.CreateVersion7(),
            absoluteExpiresAt: now + _options.RefreshToken.AbsoluteLifetime,
            requestContext,
            now,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var profile = MapProfile(user, authData);

        return new AuthenticationResult(
            accessToken.Token,
            accessToken.ExpiresAt,
            refreshToken.PlainToken,
            profile);
    }

    public async Task<AuthenticationResult> RefreshAsync(
        string refreshToken,
        RequestContext requestContext,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new TokenInvalidException();
        }

        var now = _timeProvider.GetUtcNow();
        var tokenHash = _tokenHasher.Hash(refreshToken);

        var existingToken = await _refreshTokenStore.FindTrackedByTokenHashAsync(tokenHash, cancellationToken)
            ?? throw new TokenInvalidException();

        if (existingToken.RevokedAt is not null)
        {
            if (existingToken.ReplacedByTokenId is not null)
            {
                await _refreshTokenStore.RevokeFamilyAsync(
                    existingToken.FamilyId,
                    now,
                    requestContext.IpAddress,
                    cancellationToken);

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                throw new RefreshTokenReuseDetectedException();
            }

            throw new TokenInvalidException();
        }

        if (!existingToken.IsActive(now))
        {
            existingToken.Revoke(now, requestContext.IpAddress);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            throw new TokenInvalidException();
        }

        var user = await _userStore.FindTrackedByIdAsync(existingToken.UserId, cancellationToken)
            ?? throw new TokenInvalidException();

        if (user.IsLockedOut(now))
        {
            await _refreshTokenStore.RevokeFamilyAsync(
                existingToken.FamilyId,
                now,
                requestContext.IpAddress,
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            throw new AccountLockedException();
        }

        if (_options.Email.RequireConfirmation && !user.IsEmailConfirmed)
        {
            await _refreshTokenStore.RevokeFamilyAsync(
                existingToken.FamilyId,
                now,
                requestContext.IpAddress,
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            throw new EmailNotConfirmedException();
        }

        var authData = await _userStore.GetAuthenticationDataAsync(user.Id, cancellationToken);

        var accessToken = _accessTokenIssuer.Issue(new AccessTokenRequest(
            UserId: user.Id,
            Email: user.Email,
            DisplayName: BuildDisplayName(user),
            Roles: authData.Roles,
            Permissions: authData.Permissions,
            SecurityStamp: user.SecurityStamp));

        var newToken = await CreateRefreshTokenAsync(
            user.Id,
            familyId: existingToken.FamilyId,
            absoluteExpiresAt: existingToken.AbsoluteExpiresAt,
            requestContext,
            now,
            cancellationToken);

        existingToken.ReplaceWith(newToken.Token.Id, now, requestContext.IpAddress);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var profile = MapProfile(user, authData);

        return new AuthenticationResult(
            accessToken.Token,
            accessToken.ExpiresAt,
            newToken.PlainToken,
            profile);
    }

    public async Task LogoutAsync(
        string? refreshToken,
        RequestContext requestContext,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var now = _timeProvider.GetUtcNow();
        var tokenHash = _tokenHasher.Hash(refreshToken);

        var token = await _refreshTokenStore.FindTrackedByTokenHashAsync(tokenHash, cancellationToken);

        if (token is null)
        {
            return;
        }

        await _refreshTokenStore.RevokeFamilyAsync(
            token.FamilyId,
            now,
            requestContext.IpAddress,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<SimpleResult> ForgotPasswordAsync(
        ForgotPasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var normalizedEmail = EmailNormalizer.Normalize(command.Email);

        var user = await _userStore.FindTrackedByEmailAsync(normalizedEmail, cancellationToken);

        if (user is null || (_options.Email.RequireConfirmation && !user.IsEmailConfirmed))
        {
            return new SimpleResult(true, "If the email exists, a password reset link has been sent.");
        }

        var token = _tokenGenerator.Generate();
        var tokenHash = _tokenHasher.Hash(token);
        var expiresAt = now + _options.Email.PasswordResetLifetime;

        user.SetPasswordResetToken(tokenHash, expiresAt, now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await TrySendPasswordResetEmailAsync(user.Email, token, cancellationToken);

        return new SimpleResult(true, "If the email exists, a password reset link has been sent.");
    }

    public async Task<SimpleResult> ResetPasswordAsync(
        ResetPasswordCommand command,
        RequestContext requestContext,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var tokenHash = _tokenHasher.Hash(command.Token);

        var user = await _userStore.FindTrackedByPasswordResetTokenHashAsync(tokenHash, cancellationToken)
            ?? throw new TokenInvalidException();

        if (user.PasswordResetTokenExpiresAt is null || user.PasswordResetTokenExpiresAt <= now)
        {
            user.ClearPasswordResetToken(now);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            throw new TokenInvalidException();
        }

        var passwordHash = _passwordHasher.Hash(command.Password);

        user.SetPasswordHash(passwordHash, now);
        user.ClearPasswordResetToken(now);

        await _refreshTokenStore.RevokeAllForUserAsync(
            user.Id,
            now,
            requestContext.IpAddress,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new SimpleResult(true, "Password reset successfully.");
    }

    public async Task<SimpleResult> ConfirmEmailAsync(
        ConfirmEmailCommand command,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var tokenHash = _tokenHasher.Hash(command.Token);

        var user = await _userStore.FindTrackedByEmailConfirmationTokenHashAsync(tokenHash, cancellationToken)
            ?? throw new TokenInvalidException();

        user.ConfirmEmail(now);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new SimpleResult(true, "Email confirmed successfully.");
    }

    public async Task<UserProfile> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var user = await _userStore.FindTrackedByIdAsync(userId, cancellationToken)
            ?? throw new UnauthorizedAccessException("User was not found.");

        var authData = await _userStore.GetAuthenticationDataAsync(user.Id, cancellationToken);

        return MapProfile(user, authData);
    }

    private async Task<(RefreshToken Token, string PlainToken)> CreateRefreshTokenAsync(
        Guid userId,
        Guid familyId,
        DateTimeOffset absoluteExpiresAt,
        RequestContext requestContext,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var plainToken = _tokenGenerator.Generate();
        var tokenHash = _tokenHasher.Hash(plainToken);
        var expiresAt = now + _options.RefreshToken.SlidingLifetime;

        var token = RefreshToken.Create(
            userId: userId,
            tokenHash: tokenHash,
            familyId: familyId,
            createdAt: now,
            expiresAt: expiresAt,
            absoluteExpiresAt: absoluteExpiresAt,
            ipAddress: requestContext.IpAddress,
            userAgent: requestContext.UserAgent);

        await _refreshTokenStore.AddAsync(token, cancellationToken);

        return (token, plainToken);
    }

    private async Task TrySendEmailConfirmationAsync(
        string email,
        string token,
        CancellationToken cancellationToken)
    {
        try
        {
            var link = _emailLinkBuilder.BuildEmailConfirmationLink(token);

            var html = $"""
                <h2>Confirm your email</h2>
                <p>Use the following link to confirm your account:</p>
                <p><a href="{link}">{link}</a></p>
                """;

            await _emailService.SendAsync(
                new EmailMessage(
                    To: email,
                    Subject: "Confirm your email",
                    HtmlBody: html,
                    TextBody: link),
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to send email confirmation to {Email}.", email);
        }
    }

    private async Task TrySendPasswordResetEmailAsync(
        string email,
        string token,
        CancellationToken cancellationToken)
    {
        try
        {
            var link = _emailLinkBuilder.BuildPasswordResetLink(token);

            var html = $"""
                <h2>Reset your password</h2>
                <p>Use the following link to reset your password:</p>
                <p><a href="{link}">{link}</a></p>
                """;

            await _emailService.SendAsync(
                new EmailMessage(
                    To: email,
                    Subject: "Reset your password",
                    HtmlBody: html,
                    TextBody: link),
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to send password reset email to {Email}.", email);
        }
    }

    private static UserProfile MapProfile(User user, UserAuthenticationData authData)
    {
        return new UserProfile(
            Id: user.Id,
            Email: user.Email,
            FirstName: user.FirstName,
            LastName: user.LastName,
            IsEmailConfirmed: user.IsEmailConfirmed,
            Roles: authData.Roles,
            Permissions: authData.Permissions);
    }

    private static string BuildDisplayName(User user)
    {
        var fullName = $"{user.FirstName} {user.LastName}".Trim();

        return string.IsNullOrWhiteSpace(fullName) ? user.Email : fullName;
    }
}