using MedicalAppointments.Contracts.Common;
using MedicalAppointments.Contracts.Users;

namespace MedicalAppointments.Application.Users;

public interface IUserAdminService
{
    Task<PaginatedResponse<UserResponse>> GetPagedAsync(
        UserListQuery query,
        CancellationToken cancellationToken = default);

    Task<UserResponse> GetByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<UserResponse> CreateAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken = default);

    Task<UserResponse> AssignRolesAsync(
        Guid userId,
        IEnumerable<string> roleNames,
        CancellationToken cancellationToken = default);

    Task<UserResponse> UnlockAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}

public sealed record CreateUserCommand(
    string Email,
    string Password,
    string? FirstName,
    string? LastName,
    IReadOnlyList<string> Roles);