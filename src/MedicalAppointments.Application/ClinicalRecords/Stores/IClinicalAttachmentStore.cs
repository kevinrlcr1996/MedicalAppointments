using MedicalAppointments.Domain.ClinicalRecords;

namespace MedicalAppointments.Application.ClinicalRecords.Stores;

public interface IClinicalAttachmentStore
{
    Task AddAsync(ClinicalAttachment attachment, CancellationToken cancellationToken = default);

    Task<ClinicalAttachment?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClinicalAttachment>> GetByNoteAsync(Guid noteId, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, int>> GetCountsByNoteIdsAsync(IEnumerable<Guid> noteIds, CancellationToken cancellationToken = default);
}