using MedicalAppointments.Application.ClinicalRecords.Stores;
using MedicalAppointments.Domain.ClinicalRecords;
using MedicalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedicalAppointments.Infrastructure.ClinicalRecords.Persistence;

public sealed class EfPatientAllergyStore : IPatientAllergyStore
{
    private readonly AppDbContext _dbContext;
    public EfPatientAllergyStore(AppDbContext dbContext) => _dbContext = dbContext;

    public async Task AddAsync(PatientAllergy allergy, CancellationToken cancellationToken = default)
        => await _dbContext.PatientAllergies.AddAsync(allergy, cancellationToken);

    public Task<PatientAllergy?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.PatientAllergies.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<PatientAllergy>> GetByPatientAsync(Guid patientId, CancellationToken cancellationToken = default)
        => await _dbContext.PatientAllergies
            .AsNoTracking()
            .Where(x => x.PatientId == patientId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

    // Asunción flaggeada: DbSet Patients (naming uniforme del codebase). Confirmar si diverge.
    public Task<bool> PatientExistsAsync(Guid patientId, CancellationToken cancellationToken = default)
        => _dbContext.Patients.AnyAsync(p => p.Id == patientId, cancellationToken);
}