using MedicalAppointments.Application.ClinicalRecords.Stores;
using MedicalAppointments.Domain.ClinicalRecords;
using MedicalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedicalAppointments.Infrastructure.ClinicalRecords.Persistence;

public class EfClinicalDocumentStore : IClinicalDocumentStore
{
    private readonly AppDbContext _dbContext;

    public EfClinicalDocumentStore(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(ClinicalDocument document, CancellationToken cancellationToken = default)
        => await _dbContext.ClinicalDocuments.AddAsync(document, cancellationToken);

    public async Task<IReadOnlyList<ClinicalDocument>> GetByPrescriptionAsync(Guid prescriptionId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ClinicalDocuments
            .AsNoTracking()
            .Where(x => x.PrescriptionId == prescriptionId)
            .OrderBy(x => x.GeneratedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ClinicalDocument?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.ClinicalDocuments
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}
