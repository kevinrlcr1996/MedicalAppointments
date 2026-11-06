namespace MedicalAppointments.Application.WaitingList;

public sealed class WaitingListOptions
{
    public TimeSpan OfferTtl { get; set; } = TimeSpan.FromMinutes(15);

    public int ExpirationPollingSeconds { get; set; } = 30;

    public int ExpirationBatchSize { get; set; } = 50;
}