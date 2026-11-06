using MedicalAppointments.Application.Outbox;
using MedicalAppointments.Domain.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MedicalAppointments.Infrastructure.Persistence;

public sealed class EfOutboxMessageStore : IOutboxMessageStore
{
    private readonly AppDbContext _dbContext;
    private readonly IOptions<OutboxProcessingOptions> _optionsAccessor;

    public EfOutboxMessageStore(
        AppDbContext dbContext,
        IOptions<OutboxProcessingOptions> optionsAccessor)
    {
        _dbContext = dbContext;
        _optionsAccessor = optionsAccessor;
    }

    public async Task<IReadOnlyList<OutboxMessage>> AcquirePendingAsync(
        string owner,
        int batchSize,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(owner);
        ArgumentOutOfRangeException.ThrowIfLessThan(batchSize, 1);

        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using (var transaction =
                await _dbContext.Database.BeginTransactionAsync(
                    cancellationToken))
            {
                await ReclaimStaleProcessingAsync(
                    now,
                    cancellationToken);

                var ids = await _dbContext.OutboxMessages
                    .AsNoTracking()
                    .Where(x => x.Status == OutboxStatus.Pending
                        && (x.NextAttemptAt == null
                            || x.NextAttemptAt <= now))
                    .OrderBy(x => x.OccurredAt)
                    .Take(batchSize)
                    .Select(x => x.Id)
                    .ToListAsync(cancellationToken);

                if (ids.Count > 0)
                {
                    await _dbContext.OutboxMessages
                        .Where(x => ids.Contains(x.Id)
                            && x.Status == OutboxStatus.Pending)
                        .ExecuteUpdateAsync(
                            setters => setters
                                .SetProperty(
                                    x => x.Status,
                                    OutboxStatus.Processing)
                                .SetProperty(
                                    x => x.LockOwner,
                                    owner)
                                .SetProperty(
                                    x => x.LockedAt,
                                    now)
                                .SetProperty(
                                    x => x.NextAttemptAt,
                                    (DateTimeOffset?)null),
                            cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
            }

            // Retrieve only messages successfully acquired by this owner.
            return (IReadOnlyList<OutboxMessage>)
                await _dbContext.OutboxMessages
                    .Where(x =>
                        x.Status == OutboxStatus.Processing
                        && x.LockOwner == owner)
                    .OrderBy(x => x.OccurredAt)
                    .ToListAsync(cancellationToken);
        });
    }

    public Task SaveAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ReclaimStaleProcessingAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var threshold =
            now - _optionsAccessor.Value.StaleProcessingThreshold;

        await _dbContext.OutboxMessages
            .Where(x =>
                x.Status == OutboxStatus.Processing
                && x.LockedAt != null
                && x.LockedAt < threshold)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        x => x.Status,
                        OutboxStatus.Pending)
                    .SetProperty(
                        x => x.NextAttemptAt,
                        now)
                    .SetProperty(
                        x => x.LockOwner,
                        (string?)null)
                    .SetProperty(
                        x => x.LockedAt,
                        (DateTimeOffset?)null),
                cancellationToken);
    }
}
