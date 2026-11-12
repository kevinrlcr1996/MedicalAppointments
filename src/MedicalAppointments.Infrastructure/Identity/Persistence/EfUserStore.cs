using MedicalAppointments.Application.Auth;
using MedicalAppointments.Domain.Identity;
using MedicalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedicalAppointments.Infrastructure.Identity.Persistence;

public sealed class EfUserStore : IUserStore
{
    private readonly AppDbContext _dbContext;

    public EfUserStore(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.AnyAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.AnyAsync(x => x.Id == userId, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _dbContext.Users.AddAsync(user, cancellationToken);
    }

    public async Task AssignRoleByNameAsync(
        User user,
        string roleName,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(roleName);

        var normalizedRoleName = roleName.Trim().ToUpperInvariant();

        var role = await _dbContext.Roles
            .FirstOrDefaultAsync(x => x.NormalizedName == normalizedRoleName, cancellationToken);

        if (role is not null)
        {
            user.AssignRole(role.Id, now);
        }
    }

    public async Task<IReadOnlyDictionary<string, Guid>> GetRoleIdsByNamesAsync(
        IEnumerable<string> roleNames,
        CancellationToken cancellationToken = default)
    {
        var normalized = roleNames
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().ToUpperInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalized.Length == 0)
        {
            return new Dictionary<string, Guid>();
        }

        var rows = await _dbContext.Roles
            .AsNoTracking()
            .Where(r => normalized.Contains(r.NormalizedName))
            .Select(r => new { r.Name, r.Id })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.Name, r => r.Id, StringComparer.OrdinalIgnoreCase);
    }

    public Task<User?> FindTrackedByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
    }

    public Task<User?> FindTrackedByIdWithRolesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .Include(x => x.Roles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
    }

    public async Task<UserAdminPage> GetAdminPageAsync(
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var users = _dbContext.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            users = users.Where(x =>
                x.Email.Contains(term) ||
                (x.FirstName != null && x.FirstName.Contains(term)) ||
                (x.LastName != null && x.LastName.Contains(term)));
        }

        var totalCount = await users.CountAsync(cancellationToken);
        var rows = await users
            .Include(x => x.Roles)
            .ThenInclude(x => x.Role)
            .OrderBy(x => x.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(x => new UserAdminData(
                x.Id,
                x.Email,
                x.FirstName,
                x.LastName,
                x.IsEmailConfirmed,
                x.LockoutEnd,
                x.Roles.OrderBy(r => r.Role.Name).Select(r => r.Role.Name).ToArray()))
            .ToArray();

        return new UserAdminPage(items, totalCount);
    }

    public Task<User?> FindTrackedByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public Task<User?> FindTrackedByEmailConfirmationTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .FirstOrDefaultAsync(x => x.EmailConfirmationTokenHash == tokenHash, cancellationToken);
    }

    public Task<User?> FindTrackedByPasswordResetTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .FirstOrDefaultAsync(x => x.PasswordResetTokenHash == tokenHash, cancellationToken);
    }

    public async Task<UserLoginCandidate?> FindLoginCandidateByEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);

        if (user is null)
        {
            return null;
        }

        var (roles, permissions) = await LoadAuthDataAsync(user.Id, cancellationToken);

        return new UserLoginCandidate(
            user.Id,
            user.Email,
            user.PasswordHash,
            user.IsEmailConfirmed,
            user.FailedLoginCount,
            user.LockoutEnd,
            user.SecurityStamp,
            roles,
            permissions);
    }

    public async Task<UserAuthenticationData> GetAuthenticationDataAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var (roles, permissions) = await LoadAuthDataAsync(userId, cancellationToken);

        return new UserAuthenticationData(roles, permissions);
    }

    public async Task<UserProfile?> GetProfileAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        var (roles, permissions) = await LoadAuthDataAsync(userId, cancellationToken);

        return new UserProfile(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.IsEmailConfirmed,
            roles,
            permissions);
    }

    // CLAVE DEL FIX: los permisos del usuario = heredados del rol + directos, deduplicados.
    // Se devuelve Permission.Name (lowercase) para que coincida con las constantes del
    // backend (Permissions.*) y del frontend (lib/permissions.ts). Usar NormalizedName
    // (uppercase) rompería tanto hasPermission() en React como principal.HasPermission()
    // en el handler de autorización.
    private async Task<(List<string> Roles, List<string> Permissions)> LoadAuthDataAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var roles = await _dbContext.UserRoles
            .Where(ur => ur.UserId == userId)
            .Join(
                _dbContext.Roles,
                ur => ur.RoleId,
                r => r.Id,
                (ur, r) => r.Name)
            .Distinct()
            .ToListAsync(cancellationToken);

        var rolePermissionNames = await _dbContext.UserRoles
            .Where(ur => ur.UserId == userId)
            .Join(
                _dbContext.RolePermissions,
                ur => ur.RoleId,
                rp => rp.RoleId,
                (ur, rp) => rp.PermissionId)
            .Join(
                _dbContext.Permissions,
                permissionId => permissionId,
                p => p.Id,
                (permissionId, p) => p.Name)
            .ToListAsync(cancellationToken);

        var directPermissionNames = await _dbContext.UserPermissions
            .Where(up => up.UserId == userId)
            .Join(
                _dbContext.Permissions,
                up => up.PermissionId,
                p => p.Id,
                (up, p) => p.Name)
            .ToListAsync(cancellationToken);

        var permissions = rolePermissionNames
            .Concat(directPermissionNames)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return (roles, permissions);
    }
}