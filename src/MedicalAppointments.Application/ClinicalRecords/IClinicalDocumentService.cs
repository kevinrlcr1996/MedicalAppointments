using MedicalAppointments.Application.ClinicalRecords.Common;

namespace MedicalAppointments.Application.ClinicalRecords;

public interface IClinicalDocumentService
{
    Task<IReadOnlyList<ClinicalDocumentModel>> ListByPrescriptionAsync(Guid prescriptionId, CancellationToken cancellationToken = default);
    Task<ClinicalDocumentModel> GeneratePrescriptionDocumentAsync(Guid prescriptionId, CancellationToken cancellationToken = default);
    Task<(Stream Content, string FileName, string ContentType)> DownloadAsync(Guid documentId, CancellationToken cancellationToken = default);
}
