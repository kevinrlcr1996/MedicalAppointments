using MedicalAppointments.Application.Common;
using MedicalAppointments.Domain.MedicalStructure;

namespace MedicalAppointments.Application.MedicalStructure.Resources;

public interface IResourceStore
{
    Task<bool> ExistsNormalizedNameInClinicAsync(
        Guid clinicId,
        string normalizedName,
        Guid? excludeId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsNormalizedCodeInClinicAsync(
        Guid clinicId,
        string normalizedCode,
        Guid? excludeId,
        CancellationToken cancellationToken = default);

    Task AddAsync(Resource resource, CancellationToken cancellationToken = default);

    Task<Resource?> GetTrackedByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PagedResult<Resource>> GetPagedAsync(
        ResourceQuery query,
        CancellationToken cancellationToken = default);
}