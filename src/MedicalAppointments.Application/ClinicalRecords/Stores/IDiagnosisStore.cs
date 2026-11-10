using MedicalAppointments.Domain.ClinicalRecords;

namespace MedicalAppointments.Application.ClinicalRecords.Stores;

public interface IDiagnosisStore
{
    Task AddAsync(Diagnosis diagnosis, CancellationToken cancellationToken = default);
    Task<Diagnosis?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Diagnosis>> GetByAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default);
}