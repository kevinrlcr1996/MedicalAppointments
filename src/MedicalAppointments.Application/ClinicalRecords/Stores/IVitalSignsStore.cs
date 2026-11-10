using MedicalAppointments.Domain.ClinicalRecords;

namespace MedicalAppointments.Application.ClinicalRecords.Stores;

public interface IVitalSignsStore
{
    Task AddAsync(VitalSigns vitalSigns, CancellationToken cancellationToken = default);
    Task<VitalSigns?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VitalSigns>> GetByAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default);
}