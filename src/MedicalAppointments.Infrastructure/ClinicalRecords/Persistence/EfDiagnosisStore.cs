using MedicalAppointments.Application.ClinicalRecords.Stores;
using MedicalAppointments.Domain.ClinicalRecords;
using MedicalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedicalAppointments.Infrastructure.ClinicalRecords.Persistence;

public sealed class EfDiagnosisStore : IDiagnosisStore
{
    private readonly AppDbContext _dbContext;
    public EfDiagnosisStore(AppDbContext dbContext) => _dbContext = dbContext;

    public async Task AddAsync(Diagnosis diagnosis, CancellationToken cancellationToken = default)
        => await _dbContext.Diagnoses.AddAsync(diagnosis, cancellationToken);

    public Task<Diagnosis?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.Diagnoses.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Diagnosis>> GetByAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default)
        => await _dbContext.Diagnoses
            .AsNoTracking()
            .Where(x => x.AppointmentId == appointmentId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
}