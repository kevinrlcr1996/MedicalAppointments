using MedicalAppointments.Application.Common;
using MedicalAppointments.Domain.MedicalStructure;

namespace MedicalAppointments.Application.MedicalStructure.Specialties;

public interface ISpecialtyStore
{
    Task<bool> ExistsNormalizedNameAsync(
        string normalizedName,
        Guid? excludeId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsNormalizedCodeAsync(
        string normalizedCode,
        Guid? excludeId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default);

    Task AddAsync(Specialty specialty, CancellationToken cancellationToken = default);

    Task<Specialty?> GetTrackedByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PagedResult<Specialty>> GetPagedAsync(
        SpecialtyQuery query,
        CancellationToken cancellationToken = default);
}