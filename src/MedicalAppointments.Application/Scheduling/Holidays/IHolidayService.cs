using MedicalAppointments.Application.Common;

namespace MedicalAppointments.Application.Scheduling.Holidays;

public interface IHolidayService
{
    Task<PagedResult<HolidayDto>> GetPagedAsync(
        HolidayQuery query,
        CancellationToken cancellationToken = default);

    Task<HolidayDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<HolidayDto> CreateAsync(
        CreateHolidayCommand command,
        CancellationToken cancellationToken = default);

    Task<HolidayDto> UpdateAsync(
        UpdateHolidayCommand command,
        CancellationToken cancellationToken = default);

    Task<HolidayDto> SetActiveAsync(
        SetHolidayActiveCommand command,
        CancellationToken cancellationToken = default);
}