using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.MedicalStructure.Doctors;
using MedicalAppointments.Domain.MedicalStructure;
using MedicalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedicalAppointments.Infrastructure.MedicalStructure.Persistence;

public sealed class EfDoctorStore : IDoctorStore
{
    private readonly AppDbContext _dbContext;

    public EfDoctorStore(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsNormalizedLicenseAsync(
        string normalizedLicenseNumber,
        string normalizedLicenseCountry,
        Guid? excludeId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Doctors.AnyAsync(
            x => x.NormalizedLicenseNumber == normalizedLicenseNumber
                && x.NormalizedLicenseCountry == normalizedLicenseCountry
                && (excludeId == null || x.Id != excludeId),
            cancellationToken);
    }

    public Task<bool> ExistsUserIdAsync(
        Guid userId,
        Guid? excludeId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Doctors.AnyAsync(
            x => x.UserId == userId && (excludeId == null || x.Id != excludeId),
            cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToArray();

        if (idList.Length == 0)
        {
            return true;
        }

        var count = await _dbContext.Doctors
            .AsNoTracking()
            .CountAsync(x => idList.Contains(x.Id), cancellationToken);

        return count == idList.Length;
    }

    public Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Doctors.AnyAsync(x => x.Id == id, cancellationToken);
    }

    public Task<Guid?> GetUserIdAsync(
        Guid doctorId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Doctors
            .AsNoTracking()
            .Where(x => x.Id == doctorId)
            .Select(x => x.UserId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<Guid?> GetIdByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Doctors
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(Doctor doctor, CancellationToken cancellationToken = default)
    {
        await _dbContext.Doctors.AddAsync(doctor, cancellationToken);
    }

    public Task<Doctor?> GetTrackedWithRelationsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Doctors
            .Include(x => x.Specialties).ThenInclude(s => s.Specialty)
            .Include(x => x.Clinics).ThenInclude(c => c.Clinic)
            .Include(x => x.Languages)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<PagedResult<Doctor>> GetPagedAsync(
        DoctorQuery query,
        CancellationToken cancellationToken = default)
    {
        var q = _dbContext.Doctors.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToUpperInvariant();
            var raw = query.Search.Trim();

            q = q.Where(x =>
                x.NormalizedFullName.Contains(search) ||
                (x.Email != null && x.Email.Contains(raw)) ||
                x.NormalizedLicenseNumber.Contains(search));
        }

        if (query.SpecialtyId.HasValue)
        {
            q = q.Where(x => x.Specialties.Any(s => s.SpecialtyId == query.SpecialtyId.Value));
        }

        if (query.ClinicId.HasValue)
        {
            q = q.Where(x => x.Clinics.Any(c => c.ClinicId == query.ClinicId.Value));
        }

        if (query.Status.HasValue)
        {
            q = q.Where(x => x.Status == query.Status.Value);
        }

        q = q.OrderBy(x => x.NormalizedFullName);

        // FIX: cargar colecciones para que DoctorService.Map rellene clinics/specialties/languages.
        // AsSplitQuery evita cartesian explosion al incluir 3 colecciones en una query paginada.
        return await q
            .Include(x => x.Specialties).ThenInclude(s => s.Specialty)
            .Include(x => x.Clinics).ThenInclude(c => c.Clinic)
            .Include(x => x.Languages)
            .AsSplitQuery()
            .ToPagedResultAsync(query.Page, query.PageSize, cancellationToken);
    }
}