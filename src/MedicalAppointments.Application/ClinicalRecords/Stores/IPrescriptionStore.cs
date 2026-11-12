using MedicalAppointments.Domain.ClinicalRecords;

namespace MedicalAppointments.Application.ClinicalRecords.Stores;

public interface IPrescriptionStore
{
    Task AddAsync(Prescription prescription, CancellationToken cancellationToken = default);
    Task<Prescription?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Prescription?> GetTrackedByIdWithPatientAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Prescription>> GetByPatientAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Prescription>> GetOperationalByPatientAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<bool> PatientExistsAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<bool> HasAppointmentWithDoctorAsync(Guid patientId, Guid doctorId, CancellationToken cancellationToken = default);
}
