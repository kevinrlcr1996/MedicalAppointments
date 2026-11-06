using MedicalAppointments.Application.Common;

namespace MedicalAppointments.Application.Appointments;

public interface IAppointmentQueryService
{
    Task<PagedResult<AppointmentDto>> GetPagedAsync(
        AppointmentQuery query,
        CancellationToken cancellationToken = default);

    Task<AppointmentDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}