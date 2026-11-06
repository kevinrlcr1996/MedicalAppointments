using MedicalAppointments.Application.Common;

namespace MedicalAppointments.Application.MedicalStructure.Specialties;

public interface ISpecialtyService
{
    Task<PagedResult<SpecialtyDto>> GetPagedAsync(
        SpecialtyQuery query,
        CancellationToken cancellationToken = default);

    Task<SpecialtyDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<SpecialtyDto> CreateAsync(
        CreateSpecialtyCommand command,
        CancellationToken cancellationToken = default);

    Task<SpecialtyDto> UpdateAsync(
        UpdateSpecialtyCommand command,
        CancellationToken cancellationToken = default);

    Task<SpecialtyDto> SetActiveAsync(
        SetSpecialtyActiveCommand command,
        CancellationToken cancellationToken = default);
}