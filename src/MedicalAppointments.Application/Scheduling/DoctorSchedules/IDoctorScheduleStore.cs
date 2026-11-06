using MedicalAppointments.Application.Common;
using MedicalAppointments.Domain.Scheduling;

namespace MedicalAppointments.Application.Scheduling.DoctorSchedules;

public interface IDoctorScheduleStore
{
    Task AddAsync(DoctorSchedule schedule, CancellationToken cancellationToken = default);

    Task<DoctorSchedule?> GetTrackedWithDaysAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<DoctorSchedule?> GetActiveForDateAsync(
        Guid doctorId,
        Guid clinicId,
        DateOnly date,
        CancellationToken cancellationToken = default);

    Task<PagedResult<DoctorSchedule>> GetPagedAsync(
        DoctorScheduleQuery query,
        CancellationToken cancellationToken = default);
}