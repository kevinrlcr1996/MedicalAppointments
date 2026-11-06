namespace MedicalAppointments.Application.Scheduling.Availability;

public interface IAvailabilityService
{
    Task<AvailabilityDayDto> GetDayAsync(
        AvailabilityQuery query,
        CancellationToken cancellationToken = default);
}