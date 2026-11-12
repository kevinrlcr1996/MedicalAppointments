using MedicalAppointments.Domain.ClinicalRecords;

namespace MedicalAppointments.Application.ClinicalRecords.Auth;

public sealed record PrescriptionsAccessSubject(
    Guid? UserId, Guid? DoctorId, bool CanRead, bool CanCreate, bool CanIssue,
    bool CanCorrect, bool CanRenew, bool CanCancel, bool CanManageAll, bool CanReadAll,
    bool CanDispense, bool CanExpire,
    bool CanReadDocuments, bool CanManageDocuments);

public interface IPrescriptionsAccessService
{
    Task<PrescriptionsAccessSubject> GetSubjectAsync(CancellationToken cancellationToken = default);
    bool CanReadPrescriptions(PrescriptionsAccessSubject subject);
    bool CanReadOperationalPrescriptions(PrescriptionsAccessSubject subject);
    bool CanCreateForPatient(PrescriptionsAccessSubject subject);
    bool CanUpdateDraft(Prescription prescription, PrescriptionsAccessSubject subject);
    bool CanIssue(Prescription prescription, PrescriptionsAccessSubject subject);
    bool CanCorrect(Prescription original, PrescriptionsAccessSubject subject);
    bool CanRenew(Prescription original, PrescriptionsAccessSubject subject);
    bool CanCancel(Prescription prescription, PrescriptionsAccessSubject subject);
    bool CanDispense(Prescription prescription, PrescriptionsAccessSubject subject);
    bool CanExpire(Prescription prescription, PrescriptionsAccessSubject subject);
    bool CanReadDocuments(Prescription prescription, PrescriptionsAccessSubject subject);
    bool CanGenerateDocuments(Prescription prescription, PrescriptionsAccessSubject subject);
}
