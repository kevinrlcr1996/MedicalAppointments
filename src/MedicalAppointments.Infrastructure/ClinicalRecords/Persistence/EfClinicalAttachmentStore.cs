using MedicalAppointments.Application.ClinicalRecords.Stores;
using MedicalAppointments.Domain.ClinicalRecords;
using MedicalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedicalAppointments.Infrastructure.ClinicalRecords.Persistence;

public sealed class EfClinicalAttachmentStore : IClinicalAttachmentStore
{
    private readonly AppDbContext _dbContext;

    public EfClinicalAttachmentStore(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ClinicalAttachment attachment, CancellationToken cancellationToken = default)
    {
        await _dbContext.ClinicalAttachments.AddAsync(attachment, cancellationToken);
    }

    public Task<ClinicalAttachment?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.ClinicalAttachments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ClinicalAttachment>> GetByNoteAsync(Guid noteId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ClinicalAttachments
            .AsNoTracking()
            .Where(x => x.NoteId == noteId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}