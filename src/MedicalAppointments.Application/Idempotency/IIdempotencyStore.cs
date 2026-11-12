using MedicalAppointments.Domain.Idempotency;

namespace MedicalAppointments.Application.Idempotency;

public interface IIdempotencyStore
{
    Task<IdempotencyRecord?> FindAsync(
        string key,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        IdempotencyRecord record,
        CancellationToken cancellationToken = default);

    Task MarkCompletedAsync(
        Guid id,
        string responsePayload,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task<int> DeleteExpiredAsync(
        DateTimeOffset now,
        int batchSize,
        CancellationToken cancellationToken = default);
}
