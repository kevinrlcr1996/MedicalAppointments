using MedicalAppointments.Application.Outbox;
using MedicalAppointments.Domain.Outbox;

namespace MedicalAppointments.Infrastructure.Persistence;

public sealed class EfOutboxWriter : IOutboxWriter
{
    private readonly AppDbContext _dbContext;

    public EfOutboxWriter(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Enqueue(OutboxMessage message)
    {
        _dbContext.OutboxMessages.Add(message);
    }
}