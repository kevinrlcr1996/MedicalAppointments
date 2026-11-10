using MedicalAppointments.Domain.ClinicalRecords;

namespace MedicalAppointments.Application.ClinicalRecords.Stores;

public interface IPatientMedicationStore
{
    Task AddAsync(PatientMedication medication, CancellationToken cancellationToken = default);
    Task<PatientMedication?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PatientMedication>> GetByPatientAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<bool> PatientExistsAsync(Guid patientId, CancellationToken cancellationToken = default);
}