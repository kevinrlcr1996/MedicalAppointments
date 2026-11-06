using MedicalAppointments.Application.Common;

namespace MedicalAppointments.Application.Scheduling.DoctorSchedules;

public interface IDoctorScheduleService
{
    Task<PagedResult<ScheduleDto>> GetPagedAsync(
        DoctorScheduleQuery query,
        CancellationToken cancellationToken = default);

    Task<ScheduleDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ScheduleDto> CreateAsync(
        CreateDoctorScheduleCommand command,
        CancellationToken cancellationToken = default);

    Task<ScheduleDto> UpdateAsync(
        UpdateDoctorScheduleCommand command,
        CancellationToken cancellationToken = default);

    Task<ScheduleDto> SetActiveAsync(
        SetDoctorScheduleActiveCommand command,
        CancellationToken cancellationToken = default);
}