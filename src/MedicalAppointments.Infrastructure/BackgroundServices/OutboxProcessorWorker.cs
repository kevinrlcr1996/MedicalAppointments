using MedicalAppointments.Application.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MedicalAppointments.Infrastructure.BackgroundServices;

public sealed class OutboxProcessorWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<OutboxWorkerOptions> _optionsAccessor;
    private readonly ILogger<OutboxProcessorWorker> _logger;
    private readonly string _ownerId = $"{Environment.MachineName}-{Guid.CreateVersion7()}";

    public OutboxProcessorWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<OutboxWorkerOptions> optionsAccessor,
        ILogger<OutboxProcessorWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _optionsAccessor = optionsAccessor;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _optionsAccessor.Value;
        var pollingInterval = TimeSpan.FromSeconds(Math.Max(1, options.PollingIntervalSeconds));
        var errorDelay = TimeSpan.FromSeconds(Math.Max(1, options.ErrorDelaySeconds));

        using var timer = new PeriodicTimer(pollingInterval);

        _logger.LogInformation("Outbox processor worker starting. Owner: {OwnerId}", _ownerId);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider.GetRequiredService<IOutboxProcessor>();

                var processed = await processor.ProcessPendingAsync(_ownerId, stoppingToken);

                if (processed > 0)
                {
                    _logger.LogInformation("Processed {Count} outbox messages.", processed);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unhandled error in outbox processor worker.");
                await Task.Delay(errorDelay, stoppingToken);
            }

            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("Outbox processor worker stopping.");
    }
}