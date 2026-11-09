using MedicalAppointments.Application.ClinicalRecords.Common;

namespace MedicalAppointments.Application.ClinicalRecords;

public interface IClinicalRecordsService
{
    Task<IReadOnlyList<ClinicalNoteDto>> GetNotesByAppointmentAsync(
        Guid appointmentId,
        CancellationToken cancellationToken = default);

    Task<ClinicalNoteDto> CreateNoteAsync(
        CreateClinicalNoteCommand command,
        CancellationToken cancellationToken = default);

    Task<ClinicalNoteDto> UpdateNoteAsync(
        UpdateClinicalNoteCommand command,
        CancellationToken cancellationToken = default);

    Task<ClinicalNoteDto> FinalizeNoteAsync(
        FinalizeClinicalNoteCommand command,
        CancellationToken cancellationToken = default);

    Task<ClinicalNoteDto> CorrectNoteAsync(
        CorrectClinicalNoteCommand command,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClinicalHistoryItemDto>> GetPatientHistoryAsync(
        Guid patientId,
        CancellationToken cancellationToken = default);
}