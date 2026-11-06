namespace MedicalAppointments.Application.Outbox;

public interface IOutboxProcessor
{
    Task<int> ProcessPendingAsync(
        string ownerId,
        CancellationToken cancellationToken = default);
}