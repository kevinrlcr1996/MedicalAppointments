using MedicalAppointments.Domain.Outbox;

namespace MedicalAppointments.Application.Outbox;

public interface IOutboxHandler
{
    string SupportedType { get; }

    Task HandleAsync(OutboxMessage message, CancellationToken cancellationToken = default);
}