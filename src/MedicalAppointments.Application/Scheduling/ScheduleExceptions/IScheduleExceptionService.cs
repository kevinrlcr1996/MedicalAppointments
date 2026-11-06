using MedicalAppointments.Application.Common;

namespace MedicalAppointments.Application.Scheduling.ScheduleExceptions;

public interface IScheduleExceptionService
{
    Task<PagedResult<ScheduleExceptionDto>> GetPagedAsync(
        ScheduleExceptionQuery query,
        CancellationToken cancellationToken = default);

    Task<ScheduleExceptionDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ScheduleExceptionDto> CreateAsync(
        CreateScheduleExceptionCommand command,
        CancellationToken cancellationToken = default);

    Task<ScheduleExceptionDto> UpdateAsync(
        UpdateScheduleExceptionCommand command,
        CancellationToken cancellationToken = default);

    Task<ScheduleExceptionDto> SetActiveAsync(
        SetScheduleExceptionActiveCommand command,
        CancellationToken cancellationToken = default);
}