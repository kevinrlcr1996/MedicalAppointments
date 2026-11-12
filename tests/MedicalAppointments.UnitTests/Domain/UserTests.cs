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

    [Fact]
    public void Unlock_Should_Clear_Lockout_State()
    {
        var now = DateTimeOffset.UtcNow;
        var user = User.Create(
            "locked@example.com",
            "LOCKED@EXAMPLE.COM",
            "hash",
            null,
            null,
            true,
            now);

        user.RegisterFailedLogin(1, TimeSpan.FromMinutes(10), now);
        user.Unlock(now.AddMinutes(1));

        Assert.False(user.IsLockedOut(now.AddMinutes(1)));
        Assert.Equal(0, user.FailedLoginCount);
    }

    [Fact]
    public void ReplaceRoles_Should_Replace_And_Deduplicate_Roles()
    {
        var now = DateTimeOffset.UtcNow;
        var firstRole = Guid.NewGuid();
        var secondRole = Guid.NewGuid();
        var user = User.Create(
            "roles@example.com",
            "ROLES@EXAMPLE.COM",
            "hash",
            null,
            null,
            true,
            now);

        user.AssignRole(firstRole, now);
        user.ReplaceRoles(new[] { secondRole, secondRole }, now.AddMinutes(1));

        Assert.Single(user.Roles);
        Assert.Equal(secondRole, user.Roles.Single().RoleId);
        Assert.DoesNotContain(user.Roles, role => role.RoleId == firstRole);
    }
}