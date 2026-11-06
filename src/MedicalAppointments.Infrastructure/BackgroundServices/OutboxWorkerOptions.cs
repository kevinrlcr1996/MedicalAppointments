namespace MedicalAppointments.Infrastructure.BackgroundServices;

public sealed class OutboxWorkerOptions
{
    public int PollingIntervalSeconds { get; set; } = 2;

    public int ErrorDelaySeconds { get; set; } = 10;
}