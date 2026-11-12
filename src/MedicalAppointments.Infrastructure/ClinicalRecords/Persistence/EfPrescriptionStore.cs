using MedicalAppointments.Application.ClinicalRecords.Stores;
using MedicalAppointments.Domain.ClinicalRecords;
using MedicalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedicalAppointments.Infrastructure.ClinicalRecords.Persistence;

public sealed class EfPrescriptionStore : IPrescriptionStore
{
    private readonly AppDbContext _dbContext;
    public EfPrescriptionStore(AppDbContext dbContext) => _dbContext = dbContext;
    public async Task AddAsync(Prescription prescription, CancellationToken cancellationToken = default) => await _dbContext.Prescriptions.AddAsync(prescription, cancellationToken);
    public Task<Prescription?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default) => _dbContext.Prescriptions.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    public Task<Prescription?> GetTrackedByIdWithPatientAsync(Guid id, CancellationToken cancellationToken = default) => _dbContext.Prescriptions.Include(x => x.Patient).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    public async Task<IReadOnlyList<Prescription>> GetByPatientAsync(Guid patientId, CancellationToken cancellationToken = default)
        => await _dbContext.Prescriptions.AsNoTracking().Where(x => x.PatientId == patientId).OrderBy(x => x.CreatedAt).ToListAsync(cancellationToken);
    public async Task<IReadOnlyList<Prescription>> GetOperationalByPatientAsync(Guid patientId, CancellationToken cancellationToken = default)
        => await _dbContext.Prescriptions.AsNoTracking()
            .Where(x => x.PatientId == patientId &&
                (x.Status == PrescriptionStatus.Issued || x.Status == PrescriptionStatus.Dispensed))
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    public Task<bool> HasAppointmentWithDoctorAsync(Guid patientId, Guid doctorId, CancellationToken cancellationToken = default)
        => _dbContext.Appointments.AnyAsync(x =>
            x.PatientId == patientId &&
            x.DoctorId == doctorId &&
            (x.Status == Domain.Appointments.AppointmentStatus.Confirmed ||
             x.Status == Domain.Appointments.AppointmentStatus.CheckedIn ||
             x.Status == Domain.Appointments.AppointmentStatus.InProgress ||
             x.Status == Domain.Appointments.AppointmentStatus.Completed ||
             x.Status == Domain.Appointments.AppointmentStatus.Rescheduled),
            cancellationToken);
    public Task<bool> PatientExistsAsync(Guid patientId, CancellationToken cancellationToken = default) => _dbContext.Patients.AnyAsync(p => p.Id == patientId, cancellationToken);
}
