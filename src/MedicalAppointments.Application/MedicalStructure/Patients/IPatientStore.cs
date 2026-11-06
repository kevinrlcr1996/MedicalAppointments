using MedicalAppointments.Application.Common;
using MedicalAppointments.Domain.MedicalStructure;

namespace MedicalAppointments.Application.MedicalStructure.Patients;

public interface IPatientStore
{
    Task<bool> ExistsNormalizedIdentificationAsync(
        string normalizedIdentification,
        Guid? excludeId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsUserIdAsync(
        Guid userId,
        Guid? excludeId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);

    Task<Guid?> GetIdByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Guid?> GetUserIdAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);

    Task AddAsync(Patient patient, CancellationToken cancellationToken = default);

    Task<Patient?> GetTrackedByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PagedResult<Patient>> GetPagedAsync(
        PatientQuery query,
        CancellationToken cancellationToken = default);
}