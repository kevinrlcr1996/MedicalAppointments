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
            //.Where(x => x.NoteId == noteId)
            .Where(x => x.NoteId == noteId && !x.IsDeleted)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    // GetTrackedByIdAsync: devolver TAMBIÉN borrados (el servicio decide si permitir descargar/borrar;
    // un borrado NO debe descargarse -> el servicio lanza 404/409 al ver IsDeleted).
    // (La firma existente ya devuelve tracked sin filtro; no la cambio. Solo añado el conteo:)

    public async Task<IReadOnlyDictionary<Guid, int>> GetCountsByNoteIdsAsync(
        IEnumerable<Guid> noteIds,
        CancellationToken cancellationToken = default)
    {
        var ids = noteIds?.Distinct().ToArray() ?? Array.Empty<Guid>();
        if (ids.Length == 0)
        {
            return new Dictionary<Guid, int>();
        }

        // Conteo por nota sobre MI DbSet (la nota NO navega a adjuntos -> query separada, rule 6).
        var rows = await _dbContext.ClinicalAttachments
            .AsNoTracking()
            .Where(x => ids.Contains(x.NoteId) && !x.IsDeleted)
            .GroupBy(x => x.NoteId)
            .Select(g => new { NoteId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(r => r.NoteId, r => r.Count);
    }
}