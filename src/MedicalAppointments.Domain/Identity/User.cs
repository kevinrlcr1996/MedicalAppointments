using MedicalAppointments.Domain.Common;

namespace MedicalAppointments.Domain.Identity;

public sealed class User
{
    private User()
    {
    }

    public Guid Id { get; private set; }

    public string Email { get; private set; } = null!;

    public string NormalizedEmail { get; private set; } = null!;

    public string PasswordHash { get; private set; } = null!;

    public string? FirstName { get; private set; }

    public string? LastName { get; private set; }

    public bool IsEmailConfirmed { get; private set; }

    public string? EmailConfirmationTokenHash { get; private set; }

    public DateTimeOffset? EmailConfirmationTokenExpiresAt { get; private set; }

    public string? PasswordResetTokenHash { get; private set; }

    public DateTimeOffset? PasswordResetTokenExpiresAt { get; private set; }

    public int FailedLoginCount { get; private set; }

    public DateTimeOffset? LockoutEnd { get; private set; }

    public string SecurityStamp { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public ICollection<UserRole> Roles { get; private set; } = new List<UserRole>();

    public ICollection<UserPermission> DirectPermissions { get; private set; } = new List<UserPermission>();

    public static User Create(
        string email,
        string normalizedEmail,
        string passwordHash,
        string? firstName,
        string? lastName,
        bool isEmailConfirmed,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        return new User
        {
            Id = Guid.CreateVersion7(),
            Email = email.Trim(),
            NormalizedEmail = normalizedEmail,
            PasswordHash = passwordHash,
            FirstName = string.IsNullOrWhiteSpace(firstName) ? null : firstName.Trim(),
            LastName = string.IsNullOrWhiteSpace(lastName) ? null : lastName.Trim(),
            IsEmailConfirmed = isEmailConfirmed,
            SecurityStamp = Guid.CreateVersion7().ToString("N"),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public bool IsLockedOut(DateTimeOffset now)
    {
        return LockoutEnd.HasValue && LockoutEnd.Value > now;
    }

    public void RegisterFailedLogin(
        int maxFailedAttempts,
        TimeSpan lockoutDuration,
        DateTimeOffset now)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxFailedAttempts, 1);

        FailedLoginCount++;
        Touch(now);

        if (FailedLoginCount >= maxFailedAttempts)
        {
            LockoutEnd = now + lockoutDuration;
        }
    }

    public void ResetFailedLogin(DateTimeOffset now)
    {
        FailedLoginCount = 0;
        LockoutEnd = null;
        Touch(now);
    }

    public void SetPasswordHash(string passwordHash, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        PasswordHash = passwordHash;
        SecurityStamp = Guid.CreateVersion7().ToString("N");
        FailedLoginCount = 0;
        LockoutEnd = null;
        Touch(now);
    }

    public void SetEmailConfirmationToken(
        string tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset now)
    {
        if (IsEmailConfirmed)
        {
            throw new DomainException("The email address is already confirmed.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        EmailConfirmationTokenHash = tokenHash;
        EmailConfirmationTokenExpiresAt = expiresAt;
        Touch(now);
    }

    public void ConfirmEmail(DateTimeOffset now)
    {
        if (IsEmailConfirmed)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(EmailConfirmationTokenHash))
        {
            throw new DomainException("Email confirmation token is missing.");
        }

        if (EmailConfirmationTokenExpiresAt is null || EmailConfirmationTokenExpiresAt <= now)
        {
            throw new DomainException("Email confirmation token is expired.");
        }

        IsEmailConfirmed = true;
        EmailConfirmationTokenHash = null;
        EmailConfirmationTokenExpiresAt = null;
        SecurityStamp = Guid.CreateVersion7().ToString("N");
        Touch(now);
    }

    public void SetPasswordResetToken(
        string tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        PasswordResetTokenHash = tokenHash;
        PasswordResetTokenExpiresAt = expiresAt;
        Touch(now);
    }

    public void ClearPasswordResetToken(DateTimeOffset now)
    {
        PasswordResetTokenHash = null;
        PasswordResetTokenExpiresAt = null;
        Touch(now);
    }

    public void Unlock(DateTimeOffset now)
    {
        FailedLoginCount = 0;
        LockoutEnd = null;
        Touch(now);
    }

    public void AssignRole(Guid roleId, DateTimeOffset now)
    {
        if (Roles.Any(x => x.RoleId == roleId))
        {
            return;
        }

        Roles.Add(new UserRole(Id, roleId, now));
        Touch(now);
    }

    public void ReplaceRoles(IEnumerable<Guid> roleIds, DateTimeOffset now)
    {
        Roles.Clear();

        foreach (var roleId in roleIds.Distinct())
        {
            Roles.Add(new UserRole(Id, roleId, now));
        }

        SecurityStamp = Guid.CreateVersion7().ToString("N");
        Touch(now);
    }

    private void Touch(DateTimeOffset now)
    {
        UpdatedAt = now;
    }
}