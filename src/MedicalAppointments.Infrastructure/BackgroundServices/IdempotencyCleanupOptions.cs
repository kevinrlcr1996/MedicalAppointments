namespace MedicalAppointments.Infrastructure.BackgroundServices;

public sealed class IdempotencyCleanupOptions
{
    public const string SectionName = "IdempotencyCleanup";

    public int PollingIntervalSeconds { get; set; } = 3600;

    public int BatchSize { get; set; } = 500;
}
