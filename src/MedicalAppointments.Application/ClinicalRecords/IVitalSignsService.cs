using MedicalAppointments.Application.ClinicalRecords.Common;

namespace MedicalAppointments.Application.ClinicalRecords;

public interface IVitalSignsService
{
    Task<IReadOnlyList<VitalSignsDto>> ListByAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default);
    Task<VitalSignsDto> CreateAsync(CreateVitalSignsCommand command, CancellationToken cancellationToken = default);
    Task<VitalSignsDto> UpdateAsync(UpdateVitalSignsCommand command, CancellationToken cancellationToken = default);
    Task<VitalSignsDto> FinalizeAsync(Guid id, CancellationToken cancellationToken = default);
    Task<VitalSignsDto> CorrectAsync(CorrectVitalSignsCommand command, CancellationToken cancellationToken = default);
}