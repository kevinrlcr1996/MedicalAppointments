using MedicalAppointments.Application.Common;
using MedicalAppointments.Domain.Scheduling;

namespace MedicalAppointments.Application.Scheduling.Holidays;

public interface IHolidayStore
{
    Task AddAsync(Holiday holiday, CancellationToken cancellationToken = default);

    Task<Holiday?> GetTrackedByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsForDateAsync(
        Guid organizationId,
        Guid clinicId,
        DateOnly date,
        CancellationToken cancellationToken = default);

    Task<PagedResult<Holiday>> GetPagedAsync(
        HolidayQuery query,
        CancellationToken cancellationToken = default);
}