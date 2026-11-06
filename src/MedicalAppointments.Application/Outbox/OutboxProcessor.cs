using MedicalAppointments.Domain.Outbox;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MedicalAppointments.Application.Outbox;

public sealed class OutboxProcessor : IOutboxProcessor
{
    private readonly IOutboxMessageStore _store;
    private readonly IEnumerable<IOutboxHandler> _handlers;
    private readonly TimeProvider _timeProvider;
    private readonly IOptions<OutboxProcessingOptions> _optionsAccessor;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(
        IOutboxMessageStore store,
        IEnumerable<IOutboxHandler> handlers,
        TimeProvider timeProvider,
        IOptions<OutboxProcessingOptions> optionsAccessor,
        ILogger<OutboxProcessor> logger)
    {
        _store = store;
        _handlers = handlers;
        _timeProvider = timeProvider;
        _optionsAccessor = optionsAccessor;
        _logger = logger;
    }

    public async Task<int> ProcessPendingAsync(string ownerId, CancellationToken cancellationToken = default)
    {
        var options = _optionsAccessor.Value;
        var now = _timeProvider.GetUtcNow();

        var messages = await _store.AcquirePendingAsync(
            ownerId,
            options.BatchSize,
            now,
            cancellationToken);

        var processed = 0;

        foreach (var message in messages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var handler = ResolveHandler(message.Type);

                if (handler is null)
                {
                    var error = $"No outbox handler registered for type '{message.Type}'.";
                    _logger.LogWarning("{Error}", error);

                    message.ScheduleRetry(
                        now,
                        error,
                        options.MaxAttempts,
                        options.MinimumBackoff,
                        options.MaximumBackoff);

                    await _store.SaveAsync(cancellationToken);
                    continue;
                }

                await handler.HandleAsync(message, cancellationToken);

                message.MarkProcessed(_timeProvider.GetUtcNow());
                await _store.SaveAsync(cancellationToken);

                processed++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to process outbox message {MessageId} of type {MessageType}.",
                    message.Id,
                    message.Type);

                message.ScheduleRetry(
                    _timeProvider.GetUtcNow(),
                    exception.Message,
                    options.MaxAttempts,
                    options.MinimumBackoff,
                    options.MaximumBackoff);

                await _store.SaveAsync(cancellationToken);
            }
        }

        return processed;
    }

    private IOutboxHandler? ResolveHandler(string type)
    {
        return _handlers.FirstOrDefault(
            handler => string.Equals(handler.SupportedType, type, StringComparison.Ordinal));
    }
}