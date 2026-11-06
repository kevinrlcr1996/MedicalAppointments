namespace MedicalAppointments.Application.Foundation;

public interface IFoundationRealtimeTestService
{
    Task<Guid> PublishTestEventAsync(
        PublishFoundationTestEventCommand command,
        CancellationToken cancellationToken = default);
}