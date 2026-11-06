using MedicalAppointments.Application.Common;

namespace MedicalAppointments.Application.MedicalStructure.Organizations;

public interface IOrganizationService
{
    Task<PagedResult<OrganizationDto>> GetPagedAsync(
        OrganizationQuery query,
        CancellationToken cancellationToken = default);

    Task<OrganizationDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<OrganizationDto> CreateAsync(
        CreateOrganizationCommand command,
        CancellationToken cancellationToken = default);

    Task<OrganizationDto> UpdateAsync(
        UpdateOrganizationCommand command,
        CancellationToken cancellationToken = default);

    Task<OrganizationDto> SetActiveAsync(
        SetOrganizationActiveCommand command,
        CancellationToken cancellationToken = default);
}