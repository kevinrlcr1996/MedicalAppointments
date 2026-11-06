using MedicalAppointments.Application.Common;
using MedicalAppointments.Domain.Scheduling;

namespace MedicalAppointments.Application.Scheduling.ScheduleExceptions;

public interface IScheduleExceptionStore
{
    Task AddAsync(ScheduleException exception, CancellationToken cancellationToken = default);

    Task<ScheduleException?> GetTrackedByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScheduleException>> GetForDateAsync(
        Guid doctorId,
        Guid clinicId,
        DateOnly date,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ScheduleException>> GetPagedAsync(
        ScheduleExceptionQuery query,
        CancellationToken cancellationToken = default);
}