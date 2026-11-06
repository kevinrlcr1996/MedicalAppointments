using MedicalAppointments.Application.Common;

namespace MedicalAppointments.Application.MedicalStructure.Clinics;

public interface IClinicService
{
    Task<PagedResult<ClinicDto>> GetPagedAsync(
        ClinicQuery query,
        CancellationToken cancellationToken = default);

    Task<ClinicDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ClinicDto> CreateAsync(
        CreateClinicCommand command,
        CancellationToken cancellationToken = default);

    Task<ClinicDto> UpdateAsync(
        UpdateClinicCommand command,
        CancellationToken cancellationToken = default);

    Task<ClinicDto> SetActiveAsync(
        SetClinicActiveCommand command,
        CancellationToken cancellationToken = default);
}