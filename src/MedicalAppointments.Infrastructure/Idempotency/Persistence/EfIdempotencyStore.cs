using MedicalAppointments.Application.Idempotency;
using MedicalAppointments.Domain.Idempotency;
using MedicalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedicalAppointments.Infrastructure.Idempotency.Persistence;

public sealed class EfIdempotencyStore : IIdempotencyStore
{
    private readonly AppDbContext _dbContext;

    public EfIdempotencyStore(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<IdempotencyRecord?> FindAsync(
        string key,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.IdempotencyRecords
            .FirstOrDefaultAsync(x => x.Key == key, cancellationToken);
    }

    public async Task AddAsync(
        IdempotencyRecord record,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.IdempotencyRecords.AddAsync(record, cancellationToken);
    }

    public Task MarkCompletedAsync(
        Guid id,
        string responsePayload,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.IdempotencyRecords
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.Status, IdempotencyStatus.Completed)
                    .SetProperty(x => x.ResponsePayload, responsePayload)
                    .SetProperty(x => x.CompletedAt, now),
                cancellationToken);
    }
}