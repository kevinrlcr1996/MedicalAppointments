using MedicalAppointments.Application.ClinicalRecords.Common;

namespace MedicalAppointments.Application.ClinicalRecords;

public interface IPrescriptionService
{
    Task<IReadOnlyList<PrescriptionDto>> ListByPatientAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<PrescriptionDto> CreateAsync(CreatePrescriptionCommand command, CancellationToken cancellationToken = default);
    Task<PrescriptionDto> UpdateAsync(UpdatePrescriptionCommand command, CancellationToken cancellationToken = default);
    Task<PrescriptionDto> IssueAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PrescriptionDto> CorrectAsync(CorrectPrescriptionCommand command, CancellationToken cancellationToken = default);
    Task<PrescriptionDto> RenewAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PrescriptionDto> CancelAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PrescriptionDto> DispenseAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PrescriptionDto> ExpireAsync(Guid id, CancellationToken cancellationToken = default);
}
