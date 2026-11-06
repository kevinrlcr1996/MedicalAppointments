using MedicalAppointments.Application.Common;

namespace MedicalAppointments.Application.MedicalStructure.Patients;

public interface IPatientService
{
    Task<PagedResult<PatientDto>> GetPagedAsync(
        PatientQuery query,
        CancellationToken cancellationToken = default);

    Task<PatientDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PatientDto?> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<PatientDto> CreateAsync(
        CreatePatientCommand command,
        CancellationToken cancellationToken = default);

    Task<PatientDto> UpdateAsync(
        UpdatePatientCommand command,
        CancellationToken cancellationToken = default);

    Task<PatientDto> CreateSelfAsync(
        CreateSelfPatientCommand command,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    Task<PatientDto> UpdateSelfAsync(
        UpdateSelfPatientCommand command,
        Guid currentUserId,
        CancellationToken cancellationToken = default);
}