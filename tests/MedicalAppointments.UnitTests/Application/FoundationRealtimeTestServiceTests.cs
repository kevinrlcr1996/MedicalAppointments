using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.Foundation;
using MedicalAppointments.Application.Outbox;
using MedicalAppointments.Domain.Outbox;

namespace MedicalAppointments.UnitTests.Application;

public class FoundationRealtimeTestServiceTests
{
    [Fact]
    public async Task PublishTestEventAsync_Should_Enqueue_Outbox_And_Save_UnitOfWork()
    {
        var outboxWriter = new FakeOutboxWriter();
        var unitOfWork = new FakeUnitOfWork();
        var validator = new PublishFoundationTestEventCommandValidator();
        var correlationProvider = new FakeCorrelationIdProvider();
        var currentUser = new FakeCurrentUserService();

        var service = new FoundationRealtimeTestService(
            outboxWriter,
            unitOfWork,
            TimeProvider.System,
            validator,
            correlationProvider,
            currentUser,
            JsonDefaults.Options);

        var command = new PublishFoundationTestEventCommand("Hola mundo");

        var id = await service.PublishTestEventAsync(command);

        Assert.NotEqual(Guid.Empty, id);
        Assert.Single(outboxWriter.Messages);
        Assert.Equal(1, unitOfWork.SaveCount);

        var message = Assert.Single(outboxWriter.Messages);
        Assert.Equal(OutboxTypes.FoundationTestEvent, message.Type);
        Assert.Contains("Hola mundo", message.Payload, StringComparison.Ordinal);
    }

    private sealed class FakeOutboxWriter : IOutboxWriter
    {
        public List<OutboxMessage> Messages { get; } = [];

        public void Enqueue(OutboxMessage message)
        {
            Messages.Add(message);
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class FakeCorrelationIdProvider : ICorrelationIdProvider
    {
        public string? CorrelationId => "test-correlation";
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public Guid? UserId => Guid.NewGuid();

        public string? UserName => "integration-test";

        public bool IsAuthenticated => true;

        public IReadOnlyCollection<string> Permissions => [];
    }
}