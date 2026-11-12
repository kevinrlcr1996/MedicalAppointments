using MedicalAppointments.Application.Idempotency;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MedicalAppointments.Infrastructure.BackgroundServices;

public sealed class IdempotencyCleanupWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly IdempotencyCleanupOptions _options;
    private readonly ILogger<IdempotencyCleanupWorker> _logger;

    public IdempotencyCleanupWorker(
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        IOptions<IdempotencyCleanupOptions> options,
        ILogger<IdempotencyCleanupWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(60, _options.PollingIntervalSeconds));
        using var timer = new PeriodicTimer(interval);

        _logger.LogInformation("Idempotency cleanup worker starting with an interval of {Interval}.", interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DeleteExpiredBatchAsync(stoppingToken);
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error cleaning expired idempotency records.");

                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), _timeProvider, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }

        _logger.LogInformation("Idempotency cleanup worker stopping.");
    }

    private async Task DeleteExpiredBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IIdempotencyStore>();
        var deleted = await store.DeleteExpiredAsync(
            _timeProvider.GetUtcNow(),
            Math.Max(1, _options.BatchSize),
            cancellationToken);

        if (deleted > 0)
        {
            _logger.LogInformation("Deleted {Count} expired idempotency records.", deleted);
        }
    }
}
