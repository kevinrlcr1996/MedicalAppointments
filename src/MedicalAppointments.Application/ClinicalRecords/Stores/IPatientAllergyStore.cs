using MedicalAppointments.Domain.ClinicalRecords;

namespace MedicalAppointments.Application.ClinicalRecords.Stores;

public interface IPatientAllergyStore
{
    Task AddAsync(PatientAllergy allergy, CancellationToken cancellationToken = default);
    Task<PatientAllergy?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PatientAllergy>> GetByPatientAsync(Guid patientId, CancellationToken cancellationToken = default);
    // Asunción flaggeada: naming del DbSet Patients (convención uniforme del codebase), NO firma de método.
    Task<bool> PatientExistsAsync(Guid patientId, CancellationToken cancellationToken = default);
}