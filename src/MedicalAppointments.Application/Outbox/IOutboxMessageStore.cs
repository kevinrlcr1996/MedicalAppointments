using MedicalAppointments.Domain.Outbox;

namespace MedicalAppointments.Application.Outbox;

public interface IOutboxMessageStore
{
    Task<IReadOnlyList<OutboxMessage>> AcquirePendingAsync(
        string owner,
        int batchSize,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task SaveAsync(CancellationToken cancellationToken = default);
}