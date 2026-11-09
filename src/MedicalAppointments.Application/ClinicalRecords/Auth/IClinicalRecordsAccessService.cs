using MedicalAppointments.Domain.Appointments;
using MedicalAppointments.Domain.ClinicalRecords;

namespace MedicalAppointments.Application.ClinicalRecords.Auth;

public sealed record ClinicalRecordsAccessSubject(
    Guid? UserId,
    Guid? DoctorId,
    bool CanReadAll,
    bool CanReadAssigned,
    bool CanCreate,
    bool CanUpdate,
    bool CanFinalize,
    bool CanCorrect,
    bool CanManageAll);

public interface IClinicalRecordsAccessService
{
    Task<ClinicalRecordsAccessSubject> GetSubjectAsync(CancellationToken cancellationToken = default);

    bool CanReadNotes(ClinicalRecordsAccessSubject subject);

    bool CanReadNotesForAppointment(Appointment appointment, ClinicalRecordsAccessSubject subject);

    bool CanCreateForAppointment(Appointment appointment, ClinicalRecordsAccessSubject subject);

    bool CanUpdateNote(ClinicalNote note, ClinicalRecordsAccessSubject subject);

    bool CanFinalizeNote(ClinicalNote note, ClinicalRecordsAccessSubject subject);

    bool CanCorrectNote(ClinicalNote original, ClinicalRecordsAccessSubject subject);
}