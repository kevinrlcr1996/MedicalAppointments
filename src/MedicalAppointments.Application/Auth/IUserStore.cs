using MedicalAppointments.Domain.Identity;

namespace MedicalAppointments.Application.Auth;

public interface IUserStore
{
    Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);

    Task AssignRoleByNameAsync(
        User user,
        string roleName,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, Guid>> GetRoleIdsByNamesAsync(
        IEnumerable<string> roleNames,
        CancellationToken cancellationToken = default);

    Task<User?> FindTrackedByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<User?> FindTrackedByIdWithRolesAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<UserAdminPage> GetAdminPageAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default);

    Task<User?> FindTrackedByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);

    Task<User?> FindTrackedByEmailConfirmationTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task<User?> FindTrackedByPasswordResetTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task<UserLoginCandidate?> FindLoginCandidateByEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default);

    Task<UserAuthenticationData> GetAuthenticationDataAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<UserProfile?> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);
}