namespace MedicalAppointments.Contracts.Users;

public sealed record UserResponse(
    Guid Id,
    string Email,
    string? FirstName,
    string? LastName,
    bool IsEmailConfirmed,
    DateTimeOffset? LockoutEnd,
    IReadOnlyList<string> Roles);

public sealed record CreateUserRequest(
    string Email,
    string Password,
    string? FirstName,
    string? LastName,
    IReadOnlyList<string> Roles);

public sealed record AssignRolesRequest(
    IReadOnlyList<string> Roles);

public sealed record UserListQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null);