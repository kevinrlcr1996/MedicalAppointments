namespace MedicalAppointments.Application.Scheduling.Common;

public interface IAppointmentOccupancyProvider
{
    Task<IReadOnlyList<TimeRange>> GetOccupiedRangesAsync(
        Guid doctorId,
        Guid clinicId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TimeRange>> GetOccupiedRoomRangesAsync(
        Guid roomId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default);
}
