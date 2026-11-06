using MedicalAppointments.Application.Common;
using MedicalAppointments.Domain.MedicalStructure;

namespace MedicalAppointments.Application.MedicalStructure.Clinics;

public interface IClinicStore
{
    Task<bool> ExistsNormalizedNameInOrganizationAsync(
        Guid organizationId,
        string normalizedName,
        Guid? excludeId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsNormalizedCodeInOrganizationAsync(
        Guid organizationId,
        string normalizedCode,
        Guid? excludeId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default);

    Task AddAsync(Clinic clinic, CancellationToken cancellationToken = default);

    Task<Clinic?> GetTrackedByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PagedResult<Clinic>> GetPagedAsync(
        ClinicQuery query,
        CancellationToken cancellationToken = default);
}