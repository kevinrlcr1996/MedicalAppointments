using MedicalAppointments.Application.ClinicalRecords.Common;

namespace MedicalAppointments.Application.ClinicalRecords;

public interface IPatientMedicationService
{
    Task<IReadOnlyList<PatientMedicationDto>> ListByPatientAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<PatientMedicationDto> CreateAsync(CreateMedicationCommand command, CancellationToken cancellationToken = default);
    Task<PatientMedicationDto> UpdateAsync(UpdateMedicationCommand command, CancellationToken cancellationToken = default);
    Task<PatientMedicationDto> FinalizeAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PatientMedicationDto> SuspendAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PatientMedicationDto> ResumeAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PatientMedicationDto> DiscontinueAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PatientMedicationDto> CorrectAsync(CorrectMedicationCommand command, CancellationToken cancellationToken = default);
}