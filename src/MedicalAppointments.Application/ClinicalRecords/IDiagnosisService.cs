using MedicalAppointments.Application.ClinicalRecords.Common;

namespace MedicalAppointments.Application.ClinicalRecords;

public interface IDiagnosisService
{
    Task<IReadOnlyList<DiagnosisDto>> ListByAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default);
    Task<DiagnosisDto> CreateAsync(CreateDiagnosisCommand command, CancellationToken cancellationToken = default);
    Task<DiagnosisDto> UpdateAsync(UpdateDiagnosisCommand command, CancellationToken cancellationToken = default);
    Task<DiagnosisDto> FinalizeAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DiagnosisDto> CorrectAsync(CorrectDiagnosisCommand command, CancellationToken cancellationToken = default);
}