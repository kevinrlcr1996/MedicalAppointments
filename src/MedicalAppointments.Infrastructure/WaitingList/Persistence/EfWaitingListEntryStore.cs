using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.WaitingList.Common;
using MedicalAppointments.Application.WaitingList.Stores;
using MedicalAppointments.Domain.WaitingList;
using MedicalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedicalAppointments.Infrastructure.WaitingList.Persistence;

public sealed class EfWaitingListEntryStore : IWaitingListEntryStore
{
    private readonly AppDbContext _dbContext;

    public EfWaitingListEntryStore(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(WaitingListEntry entry, CancellationToken cancellationToken = default)
    {
        await _dbContext.WaitingListEntries.AddAsync(entry, cancellationToken);
    }

    public Task<WaitingListEntry?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.WaitingListEntries.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<WaitingListEntry?> GetWithPatientAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.WaitingListEntries
            .AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.Doctor)
            .Include(x => x.Clinic)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<WaitingListEntry>> GetWaitingCandidatesAsync(
        Guid doctorId,
        Guid? clinicId,
        DateTimeOffset slotStartUtc,
        DateTimeOffset slotEndUtc,
        CancellationToken cancellationToken = default)
    {
        // ClinicId null en la entrada = cualquier clínica del médico.
        var q = _dbContext.WaitingListEntries
            .AsNoTracking()
            .Include(x => x.Patient)
            .Where(x => x.Status == WaitingListEntryStatus.Waiting
                && x.DoctorId == doctorId
                && (x.ClinicId == null || (clinicId != null && x.ClinicId == clinicId))
                && x.PreferredFromUtc <= slotStartUtc
                && x.PreferredToUtc >= slotEndUtc);

        return await q
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<WaitingListEntry>> GetPagedAsync(
        WaitingListEntryQuery query,
        WaitingListEntryReadFilter filter,
        CancellationToken cancellationToken = default)
    {
        var q = _dbContext.WaitingListEntries
            .AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.Doctor)
            .Include(x => x.Clinic)
            .AsQueryable();

        if (!filter.All)
        {
            if (filter.DoctorIds.Count == 0 && filter.PatientIds.Count == 0)
            {
                return new PagedResult<WaitingListEntry>([], query.Page, query.PageSize, 0, 0);
            }

            q = q.Where(x =>
                (filter.DoctorIds.Count > 0 && filter.DoctorIds.Contains(x.DoctorId)) ||
                (filter.PatientIds.Count > 0 && filter.PatientIds.Contains(x.PatientId)));
        }

        if (query.PatientId.HasValue) q = q.Where(x => x.PatientId == query.PatientId.Value);
        if (query.DoctorId.HasValue) q = q.Where(x => x.DoctorId == query.DoctorId.Value);
        if (query.ClinicId.HasValue) q = q.Where(x => x.ClinicId == query.ClinicId.Value);
        if (query.Status.HasValue) q = q.Where(x => x.Status == query.Status.Value);

        q = q.OrderByDescending(x => x.Priority).ThenBy(x => x.CreatedAt);

        return await q.ToPagedResultAsync(query.Page, query.PageSize, cancellationToken);
    }
}