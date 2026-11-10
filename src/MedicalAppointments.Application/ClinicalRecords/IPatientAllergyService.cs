using MedicalAppointments.Application.ClinicalRecords.Common;

namespace MedicalAppointments.Application.ClinicalRecords;

public interface IPatientAllergyService
{
    Task<IReadOnlyList<PatientAllergyDto>> ListByPatientAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<PatientAllergyDto> CreateAsync(CreateAllergyCommand command, CancellationToken cancellationToken = default);
    Task<PatientAllergyDto> UpdateAsync(UpdateAllergyCommand command, CancellationToken cancellationToken = default);
    Task<PatientAllergyDto> FinalizeAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PatientAllergyDto> ResolveAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PatientAllergyDto> UnresolveAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PatientAllergyDto> CorrectAsync(CorrectAllergyCommand command, CancellationToken cancellationToken = default);
}