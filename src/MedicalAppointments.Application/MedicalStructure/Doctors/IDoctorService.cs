using MedicalAppointments.Application.Common;

namespace MedicalAppointments.Application.MedicalStructure.Doctors;

public interface IDoctorService
{
    Task<PagedResult<DoctorDto>> GetPagedAsync(
        DoctorQuery query,
        CancellationToken cancellationToken = default);

    Task<DoctorDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<DoctorDto> CreateAsync(
        CreateDoctorCommand command,
        CancellationToken cancellationToken = default);

    Task<DoctorDto> UpdateAsync(
        UpdateDoctorCommand command,
        CancellationToken cancellationToken = default);
}