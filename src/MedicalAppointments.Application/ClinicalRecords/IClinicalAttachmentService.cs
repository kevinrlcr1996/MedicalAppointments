using MedicalAppointments.Application.ClinicalRecords.Common;

namespace MedicalAppointments.Application.ClinicalRecords;

public interface IClinicalAttachmentService
{
    Task<IReadOnlyList<ClinicalAttachmentDto>> ListByNoteAsync(Guid noteId, CancellationToken cancellationToken = default);

    Task<ClinicalAttachmentDto> UploadAsync(UploadClinicalAttachmentCommand command, CancellationToken cancellationToken = default);

    Task<(Stream Content, string FileName, string ContentType)> DownloadAsync(Guid attachmentId, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid attachmentId, CancellationToken cancellationToken = default);
}