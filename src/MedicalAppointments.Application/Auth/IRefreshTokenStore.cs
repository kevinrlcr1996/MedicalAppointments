using MedicalAppointments.Domain.Identity;

namespace MedicalAppointments.Application.Auth;

public interface IRefreshTokenStore
{
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    Task<RefreshToken?> FindTrackedByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task RevokeFamilyAsync(
        Guid familyId,
        DateTimeOffset now,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task RevokeAllForUserAsync(
        Guid userId,
        DateTimeOffset now,
        string? ipAddress,
        CancellationToken cancellationToken = default);
}