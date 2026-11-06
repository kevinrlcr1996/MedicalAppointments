using MedicalAppointments.Domain.Outbox;

namespace MedicalAppointments.Application.Outbox;

public interface IOutboxWriter
{
    void Enqueue(OutboxMessage message);
}