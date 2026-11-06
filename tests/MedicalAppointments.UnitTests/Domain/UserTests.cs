using MedicalAppointments.Domain.Identity;
using Xunit;

namespace MedicalAppointments.UnitTests.Domain;

public class UserTests
{
    [Fact]
    public void RegisterFailedLogin_Should_Lock_After_Max_Attempts()
    {
        var now = DateTimeOffset.UtcNow;

        var user = User.Create(
            email: "test@example.com",
            normalizedEmail: "TEST@EXAMPLE.COM",
            passwordHash: "hash",
            firstName: null,
            lastName: null,
            isEmailConfirmed: true,
            now: now);

        user.RegisterFailedLogin(maxFailedAttempts: 3, lockoutDuration: TimeSpan.FromMinutes(10), now: now);
        user.RegisterFailedLogin(maxFailedAttempts: 3, lockoutDuration: TimeSpan.FromMinutes(10), now: now);

        Assert.False(user.IsLockedOut(now));

        user.RegisterFailedLogin(maxFailedAttempts: 3, lockoutDuration: TimeSpan.FromMinutes(10), now: now);

        Assert.True(user.IsLockedOut(now));
        Assert.NotNull(user.LockoutEnd);
    }

    [Fact]
    public void ResetFailedLogin_Should_Unlock()
    {
        var now = DateTimeOffset.UtcNow;

        var user = User.Create(
            email: "test@example.com",
            normalizedEmail: "TEST@EXAMPLE.COM",
            passwordHash: "hash",
            firstName: null,
            lastName: null,
            isEmailConfirmed: true,
            now: now);

        user.RegisterFailedLogin(1, TimeSpan.FromMinutes(10), now);
        user.ResetFailedLogin(now);

        Assert.False(user.IsLockedOut(now));
        Assert.Equal(0, user.FailedLoginCount);
    }
}