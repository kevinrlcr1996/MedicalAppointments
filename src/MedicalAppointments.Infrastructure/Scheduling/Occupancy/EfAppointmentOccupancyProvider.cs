using MedicalAppointments.Application.Appointments;
using MedicalAppointments.Application.Scheduling.Common;

namespace MedicalAppointments.Infrastructure.Scheduling.Occupancy;

public sealed class EfAppointmentOccupancyProvider : IAppointmentOccupancyProvider
{
    private readonly IAppointmentStore _appointmentStore;

    public EfAppointmentOccupancyProvider(IAppointmentStore appointmentStore)
    {
        _appointmentStore = appointmentStore;
    }

    public Task<IReadOnlyList<TimeRange>> GetOccupiedRangesAsync(
        Guid doctorId,
        Guid clinicId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default)
    {
        return _appointmentStore.GetOccupiedRangesAsync(
            doctorId,
            clinicId,
            fromUtc,
            toUtc,
            cancellationToken);
    }

    public Task<IReadOnlyList<TimeRange>> GetOccupiedRoomRangesAsync(
        Guid roomId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default)
    {
        return _appointmentStore.GetOccupiedRoomRangesAsync(
            roomId,
            fromUtc,
            toUtc,
            cancellationToken);
    }
}
