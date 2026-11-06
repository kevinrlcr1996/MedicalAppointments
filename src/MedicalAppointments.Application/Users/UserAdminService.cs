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
    private readonly TimeProvider _timeProvider;
    private readonly AuthOptions _options;

    public UserAdminService(
        IUserStore userStore,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        IOptions<AuthOptions> optionsAccessor)
    {
        _userStore = userStore;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _options = optionsAccessor.Value;
    }

    public async Task<PaginatedResponse<UserResponse>> GetPagedAsync(
        UserListQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        // NOTA: para Fase 2 mantenemos la proyección simple.
        // En fases futuras se movería a una store específica con query eficiente.
        throw new NotImplementedException("User listing will be completed with EF projection in the next refinement.");
    }

    public Task<UserResponse> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<UserResponse> CreateAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var normalizedEmail = EmailNormalizer.Normalize(command.Email);

        if (await _userStore.EmailExistsAsync(normalizedEmail, cancellationToken))
        {
            throw new ConflictException("A user with this email already exists.");
        }

        var passwordHash = _passwordHasher.Hash(command.Password);

        var user = User.Create(
            email: command.Email,
            normalizedEmail: normalizedEmail,
            passwordHash: passwordHash,
            firstName: command.FirstName,
            lastName: command.LastName,
            isEmailConfirmed: true,
            now: now);

        await _userStore.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UserResponse(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.IsEmailConfirmed,
            user.LockoutEnd,
            command.Roles);
    }

    public Task<UserResponse> AssignRolesAsync(
        Guid userId,
        IEnumerable<string> roleNames,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<UserResponse> UnlockAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}