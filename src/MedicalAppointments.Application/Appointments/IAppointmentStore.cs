using MedicalAppointments.Application.Appointments.Auth;
using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.Scheduling.Common;
using MedicalAppointments.Domain.Appointments;

namespace MedicalAppointments.Application.Appointments;

public interface IAppointmentStore
{
    Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default);

    Task<Appointment?> GetTrackedByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Appointment?> GetWithRelationsAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PagedResult<Appointment>> GetPagedAsync(
        AppointmentQuery query,
        AppointmentReadFilter filter,
        CancellationToken cancellationToken = default);

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

    Task<bool> ExistsActiveRoomOverlapAsync(
        Guid roomId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        Guid? excludeAppointmentId,
        CancellationToken cancellationToken = default);
}
