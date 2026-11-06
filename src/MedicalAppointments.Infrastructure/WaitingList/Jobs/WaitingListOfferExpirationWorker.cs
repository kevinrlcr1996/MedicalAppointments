using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.WaitingList;
using MedicalAppointments.Application.WaitingList.Outbox;
using MedicalAppointments.Application.WaitingList.Stores;
using MedicalAppointments.Domain.WaitingList;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MedicalAppointments.Infrastructure.WaitingList.Jobs;

public sealed class WaitingListOfferExpirationWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TimeProvider _timeProvider;
    private readonly WaitingListOptions _options;
    private readonly ILogger<WaitingListOfferExpirationWorker> _logger;

    public WaitingListOfferExpirationWorker(
        IServiceScopeFactory scopeFactory,
        TimeProvider timeProvider,
        IOptions<WaitingListOptions> optionsAccessor,
        ILogger<WaitingListOfferExpirationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _timeProvider = timeProvider;
        _options = optionsAccessor.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(5, _options.ExpirationPollingSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error expiring waiting list offers.");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var offerStore = scope.ServiceProvider.GetRequiredService<IWaitingListOfferStore>();
        var entryStore = scope.ServiceProvider.GetRequiredService<IWaitingListEntryStore>();
        var notifier = scope.ServiceProvider.GetRequiredService<IWaitingListEventNotifier>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var now = _timeProvider.GetUtcNow();
        var expired = await offerStore.GetPendingExpiredAsync(now, _options.ExpirationBatchSize, cancellationToken);

        if (expired.Count == 0)
        {
            return;
        }

        foreach (var offer in expired)
        {
            offer.MarkExpired(now);

            var entry = await entryStore.GetTrackedByIdAsync(offer.EntryId, cancellationToken);

            notifier.EnqueueUpdated(
                offer.EntryId, offer.Id, offer.PatientId, null, offer.DoctorId, offer.ClinicId,
                "OfferExpired", entry?.Status, offer.Status, now, null);

            // Re-matching al siguiente candidato para ese slot.
            notifier.EnqueueSlotFreed(
                offer.DoctorId, offer.ClinicId, offer.RoomId, offer.LocalDate,
                offer.SlotStartUtc, offer.SlotEndUtc, now, null);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Expired {Count} waiting list offers.", expired.Count);
    }
}