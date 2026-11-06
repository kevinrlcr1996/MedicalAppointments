using System.Text.Json;
using FluentValidation;
using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.Outbox;
using MedicalAppointments.Application.Realtime;
using MedicalAppointments.Domain.Outbox;

namespace MedicalAppointments.Application.Foundation;

public sealed class FoundationRealtimeTestService : IFoundationRealtimeTestService
{
    private readonly IOutboxWriter _outboxWriter;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly IValidator<PublishFoundationTestEventCommand> _validator;
    private readonly ICorrelationIdProvider _correlationIdProvider;
    private readonly ICurrentUserService _currentUser;
    private readonly JsonSerializerOptions _jsonOptions;

    public FoundationRealtimeTestService(
        IOutboxWriter outboxWriter,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        IValidator<PublishFoundationTestEventCommand> validator,
        ICorrelationIdProvider correlationIdProvider,
        ICurrentUserService currentUser,
        JsonSerializerOptions jsonOptions)
    {
        _outboxWriter = outboxWriter;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _validator = validator;
        _correlationIdProvider = correlationIdProvider;
        _currentUser = currentUser;
        _jsonOptions = jsonOptions;
    }

    public async Task<Guid> PublishTestEventAsync(
        PublishFoundationTestEventCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var now = _timeProvider.GetUtcNow();

        var payload = new FoundationTestEventPayload(
            Id: Guid.CreateVersion7(),
            Message: command.Message.Trim(),
            OccurredAt: now,
            CorrelationId: _correlationIdProvider.CorrelationId,
            TargetUserId: _currentUser.UserId);

        var serializedPayload = JsonSerializer.Serialize(payload, _jsonOptions);

        var outboxMessage = OutboxMessage.Create(
            type: OutboxTypes.FoundationTestEvent,
            payload: serializedPayload,
            occurredAt: now,
            createdAt: now);

        _outboxWriter.Enqueue(outboxMessage);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return outboxMessage.Id;
    }
}