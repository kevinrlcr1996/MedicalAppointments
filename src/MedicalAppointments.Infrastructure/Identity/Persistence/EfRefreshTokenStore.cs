using MedicalAppointments.Application.Auth;
using MedicalAppointments.Domain.Identity;
using MedicalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedicalAppointments.Infrastructure.Identity.Persistence;

public sealed class EfRefreshTokenStore : IRefreshTokenStore
{
    private readonly AppDbContext _dbContext;

    public EfRefreshTokenStore(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        await _dbContext.RefreshTokens.AddAsync(refreshToken, cancellationToken);
    }

    public Task<RefreshToken?> FindTrackedByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.RefreshTokens.FirstOrDefaultAsync(
            x => x.TokenHash == tokenHash,
            cancellationToken);
    }

    public Task RevokeFamilyAsync(
        Guid familyId,
        DateTimeOffset now,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.RefreshTokens
            .Where(x => x.FamilyId == familyId && x.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.RevokedAt, now)
                    .SetProperty(x => x.RevokedByIpAddress, ipAddress),
                cancellationToken);
    }

    public Task RevokeAllForUserAsync(
        Guid userId,
        DateTimeOffset now,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.RefreshTokens
            .Where(x => x.UserId == userId && x.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.RevokedAt, now)
                    .SetProperty(x => x.RevokedByIpAddress, ipAddress),
                cancellationToken);
    }
}