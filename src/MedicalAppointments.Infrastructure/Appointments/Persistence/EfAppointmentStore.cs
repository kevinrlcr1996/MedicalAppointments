using MedicalAppointments.Application.Appointments;
using MedicalAppointments.Application.Appointments.Auth;
using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.Scheduling.Common;
using MedicalAppointments.Domain.Appointments;
using MedicalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedicalAppointments.Infrastructure.Appointments.Persistence;

public sealed class EfAppointmentStore : IAppointmentStore
{
    private static readonly AppointmentStatus[] OccupyingStatuses =
    [
        AppointmentStatus.Pending,
        AppointmentStatus.Confirmed,
        AppointmentStatus.CheckedIn,
        AppointmentStatus.InProgress
    ];

    private readonly AppDbContext _dbContext;

    public EfAppointmentStore(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        await _dbContext.Appointments.AddAsync(appointment, cancellationToken);
    }

    public Task<Appointment?> GetTrackedByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Appointments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<Appointment?> GetWithRelationsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Appointments
            .AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.Doctor)
            .Include(x => x.Clinic)
            .Include(x => x.Room)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<PagedResult<Appointment>> GetPagedAsync(
        AppointmentQuery query,
        AppointmentReadFilter filter,
        CancellationToken cancellationToken = default)
    {
        var q = _dbContext.Appointments
            .AsNoTracking()
            .Include(x => x.Patient)
            .Include(x => x.Doctor)
            .Include(x => x.Clinic)
            .Include(x => x.Room)
            .AsQueryable();

        if (!filter.All)
        {
            if (filter.DoctorIds.Count == 0 && filter.PatientIds.Count == 0)
            {
                return new PagedResult<Appointment>([], query.Page, query.PageSize, 0, 0);
            }

            q = q.Where(x =>
                (filter.DoctorIds.Count > 0 && filter.DoctorIds.Contains(x.DoctorId)) ||
                (filter.PatientIds.Count > 0 && filter.PatientIds.Contains(x.PatientId)));
        }

        if (query.PatientId.HasValue)
        {
            q = q.Where(x => x.PatientId == query.PatientId.Value);
        }

        if (query.DoctorId.HasValue)
        {
            q = q.Where(x => x.DoctorId == query.DoctorId.Value);
        }

        if (query.ClinicId.HasValue)
        {
            q = q.Where(x => x.ClinicId == query.ClinicId.Value);
        }

        if (query.RoomId.HasValue)
        {
            q = q.Where(x => x.RoomId == query.RoomId.Value);
        }

        if (query.Status.HasValue)
        {
            q = q.Where(x => x.Status == query.Status.Value);
        }

        if (query.FromUtc.HasValue)
        {
            q = q.Where(x => x.EndAt > query.FromUtc.Value);
        }

        if (query.ToUtc.HasValue)
        {
            q = q.Where(x => x.StartAt < query.ToUtc.Value);
        }

        q = q.OrderByDescending(x => x.StartAt);

        return await q.ToPagedResultAsync(query.Page, query.PageSize, cancellationToken);
    }

    public async Task<IReadOnlyList<TimeRange>> GetOccupiedRangesAsync(
        Guid doctorId,
        Guid clinicId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default)
    {
        var ranges = await _dbContext.Appointments
            .AsNoTracking()
            .Where(x => x.DoctorId == doctorId
                && x.ClinicId == clinicId
                && OccupyingStatuses.Contains(x.Status)
                && x.StartAt < toUtc
                && x.EndAt > fromUtc)
            .Select(x => new { x.StartAt, x.EndAt })
            .ToListAsync(cancellationToken);

        return ranges
            .Select(x => new TimeRange(x.StartAt, x.EndAt))
            .ToList();
    }

    public Task<bool> ExistsActiveRoomOverlapAsync(
        Guid roomId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        Guid? excludeAppointmentId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Appointments.AnyAsync(
            x => x.RoomId == roomId
                && OccupyingStatuses.Contains(x.Status)
                && x.StartAt < endUtc
                && x.EndAt > startUtc
                && (excludeAppointmentId == null || x.Id != excludeAppointmentId),
            cancellationToken);
    }
}