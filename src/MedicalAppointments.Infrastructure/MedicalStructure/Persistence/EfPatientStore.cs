using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.MedicalStructure.Patients;
using MedicalAppointments.Domain.MedicalStructure;
using MedicalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedicalAppointments.Infrastructure.MedicalStructure.Persistence;

public sealed class EfPatientStore : IPatientStore
{
    private readonly AppDbContext _dbContext;

    public EfPatientStore(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsNormalizedIdentificationAsync(
        string normalizedIdentification,
        Guid? excludeId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Patients.AnyAsync(
            x => x.NormalizedIdentificationNumber == normalizedIdentification &&
                 (excludeId == null || x.Id != excludeId),
            cancellationToken);
    }

    public Task<bool> ExistsUserIdAsync(
        Guid userId,
        Guid? excludeId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Patients.AnyAsync(
            x => x.UserId == userId &&
                 (excludeId == null || x.Id != excludeId),
            cancellationToken);
    }

    public Task<bool> ExistsAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Patients.AnyAsync(x => x.Id == patientId, cancellationToken);
    }

    public Task<Guid?> GetIdByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Patients
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => (Guid?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<Guid?> GetUserIdAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Patients
            .AsNoTracking()
            .Where(x => x.Id == patientId)
            .Select(x => x.UserId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        await _dbContext.Patients.AddAsync(patient, cancellationToken);
    }

    public Task<Patient?> GetTrackedByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Patients.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<PagedResult<Patient>> GetPagedAsync(
        PatientQuery query,
        CancellationToken cancellationToken = default)
    {
        var q = _dbContext.Patients.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToUpperInvariant();

            q = q.Where(x =>
                x.NormalizedFullName.Contains(search) ||
                (x.NormalizedIdentificationNumber != null && x.NormalizedIdentificationNumber.Contains(search)) ||
                (x.Email != null && x.Email.Contains(query.Search.Trim())) ||
                (x.Phone != null && x.Phone.Contains(query.Search.Trim())));
        }

        q = q.OrderBy(x => x.NormalizedFullName);

        return await q.ToPagedResultAsync(query.Page, query.PageSize, cancellationToken);
    }
}