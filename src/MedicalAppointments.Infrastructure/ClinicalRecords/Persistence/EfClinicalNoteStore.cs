using MedicalAppointments.Application.ClinicalRecords.Stores;
using MedicalAppointments.Domain.ClinicalRecords;
using MedicalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedicalAppointments.Infrastructure.ClinicalRecords.Persistence;

public sealed class EfClinicalNoteStore : IClinicalNoteStore
{
    private readonly AppDbContext _dbContext;

    public EfClinicalNoteStore(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ClinicalNote note, CancellationToken cancellationToken = default)
    {
        await _dbContext.ClinicalNotes.AddAsync(note, cancellationToken);
    }

    public Task<ClinicalNote?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.ClinicalNotes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<ClinicalNote?> GetWithRelationsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.ClinicalNotes
            .Include(x => x.CorrectsNote)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ClinicalNote>> GetByAppointmentAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ClinicalNotes
            .AsNoTracking()
            .Include(x => x.CorrectsNote)
            .Where(x => x.AppointmentId == appointmentId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}