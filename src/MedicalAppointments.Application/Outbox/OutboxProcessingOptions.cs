namespace MedicalAppointments.Application.Outbox;

public sealed class OutboxProcessingOptions
{
    public int BatchSize { get; set; } = 50;

    public int MaxAttempts { get; set; } = 5;

    public int MinimumBackoffSeconds { get; set; } = 30;

    public int MaximumBackoffSeconds { get; set; } = 600;

    public int StaleProcessingThresholdSeconds { get; set; } = 300;

    public TimeSpan MinimumBackoff => TimeSpan.FromSeconds(Math.Max(1, MinimumBackoffSeconds));

    public TimeSpan MaximumBackoff => TimeSpan.FromSeconds(Math.Max(MinimumBackoffSeconds, MaximumBackoffSeconds));

    public TimeSpan StaleProcessingThreshold => TimeSpan.FromSeconds(Math.Max(1, StaleProcessingThresholdSeconds));
}