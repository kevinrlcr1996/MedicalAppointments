using MedicalAppointments.Application.Common;
using MedicalAppointments.Domain.MedicalStructure;

namespace MedicalAppointments.Application.MedicalStructure.Doctors;

public interface IDoctorStore
{
    Task<bool> ExistsNormalizedLicenseAsync(
        string normalizedLicenseNumber,
        string normalizedLicenseCountry,
        Guid? excludeId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsUserIdAsync(
        Guid userId,
        Guid? excludeId,
        CancellationToken cancellationToken = default);

    Task AddAsync(Doctor doctor, CancellationToken cancellationToken = default);

    Task<Doctor?> GetTrackedWithRelationsAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PagedResult<Doctor>> GetPagedAsync(
        DoctorQuery query,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Guid?> GetUserIdAsync(Guid doctorId, CancellationToken cancellationToken = default);
    Task<Guid?> GetIdByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}