using MedicalAppointments.Domain.ClinicalRecords;

namespace MedicalAppointments.Application.ClinicalRecords.Stores;

public interface IClinicalNoteStore
{
    Task AddAsync(ClinicalNote note, CancellationToken cancellationToken = default);

    Task<ClinicalNote?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ClinicalNote?> GetWithRelationsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClinicalNote>> GetByAppointmentAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClinicalNote>> GetHistoryByPatientAsync(
        Guid patientId,
        Guid? doctorIdFilter,
        CancellationToken cancellationToken = default);
}