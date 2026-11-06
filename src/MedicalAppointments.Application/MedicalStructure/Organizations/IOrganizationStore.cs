using MedicalAppointments.Application.Common;
using MedicalAppointments.Domain.MedicalStructure;

namespace MedicalAppointments.Application.MedicalStructure.Organizations;

public interface IOrganizationStore
{
    Task<bool> ExistsNormalizedNameAsync(
        string normalizedName,
        Guid? excludeId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsNormalizedTaxIdAsync(
        string normalizedTaxId,
        Guid? excludeId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default);

    Task AddAsync(Organization organization, CancellationToken cancellationToken = default);

    Task<Organization?> GetTrackedByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PagedResult<Organization>> GetPagedAsync(
        OrganizationQuery query,
        CancellationToken cancellationToken = default);
}