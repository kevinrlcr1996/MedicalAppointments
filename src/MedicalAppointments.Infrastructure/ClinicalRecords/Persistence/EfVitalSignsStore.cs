using MedicalAppointments.Application.ClinicalRecords.Stores;
using MedicalAppointments.Domain.ClinicalRecords;
using MedicalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedicalAppointments.Infrastructure.ClinicalRecords.Persistence;

public sealed class EfVitalSignsStore : IVitalSignsStore
{
    private readonly AppDbContext _dbContext;
    public EfVitalSignsStore(AppDbContext dbContext) => _dbContext = dbContext;

    public async Task AddAsync(VitalSigns vitalSigns, CancellationToken cancellationToken = default)
        => await _dbContext.VitalSigns.AddAsync(vitalSigns, cancellationToken);

    public Task<VitalSigns?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _dbContext.VitalSigns.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<VitalSigns>> GetByAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default)
        => await _dbContext.VitalSigns
            .AsNoTracking()
            .Where(x => x.AppointmentId == appointmentId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
}