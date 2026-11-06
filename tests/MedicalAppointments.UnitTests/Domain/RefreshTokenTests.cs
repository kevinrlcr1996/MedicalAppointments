using MedicalAppointments.Domain.Identity;
using Xunit;

namespace MedicalAppointments.UnitTests.Domain;

public class RefreshTokenTests
{
    [Fact]
    public void IsActive_Should_Be_True_For_New_Token()
    {
        var now = DateTimeOffset.UtcNow;

        var token = RefreshToken.Create(
            userId: Guid.NewGuid(),
            tokenHash: "hash",
            familyId: Guid.NewGuid(),
            createdAt: now,
            expiresAt: now.AddDays(7),
            absoluteExpiresAt: now.AddDays(30),
            ipAddress: null,
            userAgent: null);

        Assert.True(token.IsActive(now));
    }

    [Fact]
    public void ReplaceWith_Should_Revoke_And_Set_Replacement()
    {
        var now = DateTimeOffset.UtcNow;
        var replacementId = Guid.NewGuid();

        var token = RefreshToken.Create(
            userId: Guid.NewGuid(),
            tokenHash: "hash",
            familyId: Guid.NewGuid(),
            createdAt: now,
            expiresAt: now.AddDays(7),
            absoluteExpiresAt: now.AddDays(30),
            ipAddress: null,
            userAgent: null);

        token.ReplaceWith(replacementId, now, "127.0.0.1");

        Assert.NotNull(token.RevokedAt);
        Assert.Equal(replacementId, token.ReplacedByTokenId);
        Assert.False(token.IsActive(now));
    }
}