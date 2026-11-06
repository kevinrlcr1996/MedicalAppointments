using MedicalAppointments.Domain.Common;

namespace MedicalAppointments.Domain.Identity;

public sealed class RefreshToken
{
    private RefreshToken()
    {
    }

    public Guid Id { get; private set; }

    public string TokenHash { get; private set; } = null!;

    public Guid UserId { get; private set; }

    public User User { get; private set; } = null!;

    public Guid FamilyId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset AbsoluteExpiresAt { get; private set; }

    public string? CreatedByIpAddress { get; private set; }

    public string? CreatedByUserAgent { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public string? RevokedByIpAddress { get; private set; }

    public Guid? ReplacedByTokenId { get; private set; }

    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public static RefreshToken Create(
        Guid userId,
        string tokenHash,
        Guid familyId,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt,
        DateTimeOffset absoluteExpiresAt,
        string? ipAddress,
        string? userAgent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        if (absoluteExpiresAt < expiresAt)
        {
            throw new DomainException("Absolute expiry cannot be earlier than sliding expiry.");
        }

        return new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            TokenHash = tokenHash,
            UserId = userId,
            FamilyId = familyId,
            CreatedAt = createdAt,
            ExpiresAt = expiresAt,
            AbsoluteExpiresAt = absoluteExpiresAt,
            CreatedByIpAddress = string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress.Trim(),
            CreatedByUserAgent = string.IsNullOrWhiteSpace(userAgent) ? null : userAgent.Trim()
        };
    }

    public bool IsActive(DateTimeOffset now)
    {
        return RevokedAt is null
            && now < ExpiresAt
            && now < AbsoluteExpiresAt;
    }

    public void Revoke(DateTimeOffset now, string? ipAddress)
    {
        if (RevokedAt is not null)
        {
            return;
        }

        RevokedAt = now;
        RevokedByIpAddress = string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress.Trim();
        ReplacedByTokenId = null;
    }

    public void ReplaceWith(
        Guid replacementTokenId,
        DateTimeOffset now,
        string? ipAddress)
    {
        if (RevokedAt is not null)
        {
            throw new DomainException("Cannot replace an already revoked refresh token.");
        }

        RevokedAt = now;
        RevokedByIpAddress = string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress.Trim();
        ReplacedByTokenId = replacementTokenId;
    }
}