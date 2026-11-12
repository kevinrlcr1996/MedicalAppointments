using MedicalAppointments.Domain.ClinicalRecords;

namespace MedicalAppointments.Application.ClinicalRecords.Stores;

public interface IClinicalDocumentStore
{
    Task AddAsync(ClinicalDocument document, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClinicalDocument>> GetByPrescriptionAsync(Guid prescriptionId, CancellationToken cancellationToken = default);
    Task<ClinicalDocument?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
