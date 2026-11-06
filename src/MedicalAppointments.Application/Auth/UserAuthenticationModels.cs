namespace MedicalAppointments.Application.Auth;

public sealed record UserLoginCandidate(
    Guid Id,
    string Email,
    string PasswordHash,
    bool IsEmailConfirmed,
    int FailedLoginCount,
    DateTimeOffset? LockoutEnd,
    string SecurityStamp,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

public sealed record UserProfile(
    Guid Id,
    string Email,
    string? FirstName,
    string? LastName,
    bool IsEmailConfirmed,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

public sealed record UserAuthenticationData(
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);