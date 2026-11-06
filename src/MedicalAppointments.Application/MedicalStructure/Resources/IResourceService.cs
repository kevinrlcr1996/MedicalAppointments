using MedicalAppointments.Application.Common;

namespace MedicalAppointments.Application.MedicalStructure.Resources;

public interface IResourceService
{
    Task<PagedResult<ResourceDto>> GetPagedAsync(
        ResourceQuery query,
        CancellationToken cancellationToken = default);

    Task<ResourceDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ResourceDto> CreateAsync(
        CreateResourceCommand command,
        CancellationToken cancellationToken = default);

    Task<ResourceDto> UpdateAsync(
        UpdateResourceCommand command,
        CancellationToken cancellationToken = default);

    Task<ResourceDto> SetActiveAsync(
        SetResourceActiveCommand command,
        CancellationToken cancellationToken = default);
}