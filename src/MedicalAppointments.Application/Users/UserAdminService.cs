using FluentValidation;
using MedicalAppointments.Application.Auth;
using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.Common.Exceptions;
using MedicalAppointments.Contracts.Common;
using MedicalAppointments.Contracts.Users;
using MedicalAppointments.Domain.Identity;
using Microsoft.Extensions.Options;

namespace MedicalAppointments.Application.Users;

public sealed class UserAdminService : IUserAdminService
{
    private readonly IUserStore _userStore;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateUserCommand> _createValidator;
    private readonly TimeProvider _timeProvider;
    private readonly AuthOptions _options;

    public UserAdminService(
        IUserStore userStore,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        IValidator<CreateUserCommand> createValidator,
        TimeProvider timeProvider,
        IOptions<AuthOptions> optionsAccessor)
    {
        _userStore = userStore;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _timeProvider = timeProvider;
        _options = optionsAccessor.Value;
    }

    public async Task<PaginatedResponse<UserResponse>> GetPagedAsync(
        UserListQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var result = await _userStore.GetAdminPageAsync(page, pageSize, query.Search, cancellationToken);
        var items = result.Items.Select(ToResponse).ToArray();

        return new PaginatedResponse<UserResponse>(
            items,
            page,
            pageSize,
            result.TotalCount,
            (int)Math.Ceiling(result.TotalCount / (double)pageSize));
    }

    public async Task<UserResponse> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userStore.FindTrackedByIdWithRolesAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        return ToResponse(user);
    }

    public async Task<UserResponse> CreateAsync(CreateUserCommand command, CancellationToken cancellationToken = default)
    {
        await _createValidator.ValidateAndThrowAsync(command, cancellationToken);

        var now = _timeProvider.GetUtcNow();
        var normalizedEmail = EmailNormalizer.Normalize(command.Email);

        if (await _userStore.EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            throw new ConflictException("A user with this email already exists.");
        }

        var roleIds = await ResolveRoleIdsAsync(command.Roles, cancellationToken);
        var passwordHash = _passwordHasher.Hash(command.Password);

        // Cuentas creadas por administración nacen confirmadas (sin round-trip de email).
        var user = User.Create(
            email: command.Email,
            normalizedEmail: normalizedEmail,
            passwordHash: passwordHash,
            firstName: command.FirstName,
            lastName: command.LastName,
            isEmailConfirmed: true,
            now: now);

        foreach (var roleId in roleIds.Values)
        {
            user.AssignRole(roleId, now);
        }

        await _userStore.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UserResponse(
            user.Id, user.Email, user.FirstName, user.LastName,
            user.IsEmailConfirmed, user.LockoutEnd, command.Roles);
    }

    public async Task<UserResponse> AssignRolesAsync(
        Guid userId,
        IEnumerable<string> roleNames,
        CancellationToken cancellationToken = default)
    {
        var user = await _userStore.FindTrackedByIdWithRolesAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        var roleIds = await ResolveRoleIdsAsync(roleNames, cancellationToken);
        user.ReplaceRoles(roleIds.Values, _timeProvider.GetUtcNow());
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(user, roleIds.Keys.OrderBy(x => x).ToArray());
    }

    private async Task<IReadOnlyDictionary<string, Guid>> ResolveRoleIdsAsync(
    IEnumerable<string> roleNames,
    CancellationToken cancellationToken)
    {
        var names = (roleNames ?? Array.Empty<string>())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (names.Length == 0)
        {
            return new Dictionary<string, Guid>();
        }

        var map = await _userStore.GetRoleIdsByNamesAsync(names, cancellationToken);

        var missing = names.Where(n => !map.ContainsKey(n)).ToArray();
        if (missing.Length > 0)
        {
            throw new NotFoundException($"Role(s) not found: {string.Join(", ", missing)}.");
        }

        return map;
    }

    public async Task<UserResponse> UnlockAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userStore.FindTrackedByIdWithRolesAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        user.Unlock(_timeProvider.GetUtcNow());
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToResponse(user);
    }

    private static UserResponse ToResponse(UserAdminData user)
    {
        return new UserResponse(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.IsEmailConfirmed,
            user.LockoutEnd,
            user.Roles);
    }

    private static UserResponse ToResponse(User user)
    {
        return ToResponse(
            user,
            user.Roles
                .Where(x => x.Role is not null)
                .OrderBy(x => x.Role.Name)
                .Select(x => x.Role.Name)
                .ToArray());
    }

    private static UserResponse ToResponse(User user, IReadOnlyList<string> roles)
    {
        return new UserResponse(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.IsEmailConfirmed,
            user.LockoutEnd,
            roles);
    }
}