using MedicalAppointments.Application.ClinicalRecords.Stores;
using MedicalAppointments.Domain.ClinicalRecords;
using MedicalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedicalAppointments.Infrastructure.ClinicalRecords.Persistence;

public sealed class EfPatientMedicationStore : IPatientMedicationStore
{
    private readonly AppDbContext _dbContext;
    public EfPatientMedicationStore(AppDbContext dbContext) => _dbContext = dbContext;
    public async Task AddAsync(PatientMedication medication, CancellationToken cancellationToken = default) => await _dbContext.PatientMedications.AddAsync(medication, cancellationToken);
    public Task<PatientMedication?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default) => _dbContext.PatientMedications.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    public async Task<IReadOnlyList<PatientMedication>> GetByPatientAsync(Guid patientId, CancellationToken cancellationToken = default)
        => await _dbContext.PatientMedications.AsNoTracking().Where(x => x.PatientId == patientId).OrderBy(x => x.CreatedAt).ToListAsync(cancellationToken);
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