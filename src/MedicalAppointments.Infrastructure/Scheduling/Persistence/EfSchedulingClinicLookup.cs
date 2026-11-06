using MedicalAppointments.Application.Scheduling.Common;
using MedicalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedicalAppointments.Infrastructure.Scheduling.Persistence;

public sealed class EfSchedulingClinicLookup : ISchedulingClinicLookup
{
    private readonly AppDbContext _dbContext;

    public EfSchedulingClinicLookup(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SchedulingClinicContext?> GetContextAsync(
        Guid clinicId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Clinics
            .AsNoTracking()
            .Where(x => x.Id == clinicId)
            .Select(x => new SchedulingClinicContext(
                x.Id,
                x.OrganizationId,
                x.TimeZoneId,
                x.IsActive))
            .FirstOrDefaultAsync(cancellationToken);
    }
}