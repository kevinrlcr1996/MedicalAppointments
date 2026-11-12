using System.Security.Claims;
using MedicalAppointments.Application.Authorization;
using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.MedicalStructure.Doctors;
using MedicalAppointments.Domain.ClinicalRecords;

namespace MedicalAppointments.Application.ClinicalRecords.Auth;

public sealed class PrescriptionsAccessService : IPrescriptionsAccessService
{
    private readonly ICurrentPrincipalProvider _principalProvider;
    private readonly IDoctorStore _doctorStore;

    public PrescriptionsAccessService(ICurrentPrincipalProvider principalProvider, IDoctorStore doctorStore)
    { _principalProvider = principalProvider; _doctorStore = doctorStore; }

    public async Task<PrescriptionsAccessSubject> GetSubjectAsync(CancellationToken cancellationToken = default)
    {
        var principal = _principalProvider.GetCurrentPrincipal() ?? throw new UnauthorizedAccessException("User is not authenticated.");
        var userId = Guid.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var parsedUserId) ? parsedUserId : (Guid?)null;

        var canRead = principal.HasPermission(Permissions.PrescriptionsRead);
        var canReadAll = canRead && principal.HasPermission(Permissions.AppointmentsReadAll);
        var canCreate = principal.HasPermission(Permissions.PrescriptionsCreate);
        var canIssue = principal.HasPermission(Permissions.PrescriptionsIssue);
        var canCorrect = principal.HasPermission(Permissions.PrescriptionsCorrect);
        var canRenew = principal.HasPermission(Permissions.PrescriptionsRenew);
        var canCancel = principal.HasPermission(Permissions.PrescriptionsCancel);
        var canManageAll = principal.HasPermission(Permissions.PrescriptionsManageAll);
        var canDispense = principal.HasPermission(Permissions.PrescriptionsDispense);
        var canExpire = principal.HasPermission(Permissions.PrescriptionsExpire);
        var canReadDocuments = principal.HasPermission(Permissions.DocumentsRead);
        var canManageDocuments = principal.HasPermission(Permissions.DocumentsManage);

        Guid? doctorId = null;
        if (userId.HasValue && (canCreate || (canRead && !canReadAll && !canDispense && !canExpire)))
            doctorId = await _doctorStore.GetIdByUserIdAsync(userId.Value, cancellationToken);

        return new PrescriptionsAccessSubject(userId, doctorId, canRead, canCreate, canIssue, canCorrect, canRenew, canCancel, canManageAll, canReadAll, canDispense, canExpire, canReadDocuments, canManageDocuments);
    }

    public bool CanReadPrescriptions(PrescriptionsAccessSubject s) =>
        s.CanRead && (s.CanManageAll || s.CanReadAll || s.DoctorId.HasValue || s.CanDispense || s.CanExpire);
    public bool CanReadOperationalPrescriptions(PrescriptionsAccessSubject s) =>
        s.CanRead && (s.CanDispense || s.CanExpire);
    public bool CanCreateForPatient(PrescriptionsAccessSubject s) => s.CanCreate;
    public bool CanUpdateDraft(Prescription p, PrescriptionsAccessSubject s) => s.CanCreate && (s.CanManageAll || (s.UserId.HasValue && p.AuthorUserId == s.UserId.Value));
    public bool CanIssue(Prescription p, PrescriptionsAccessSubject s) => s.CanIssue && (s.CanManageAll || (s.UserId.HasValue && p.AuthorUserId == s.UserId.Value));
    public bool CanCorrect(Prescription o, PrescriptionsAccessSubject s) => s.CanCorrect && (s.CanManageAll || (s.UserId.HasValue && o.AuthorUserId == s.UserId.Value));
    public bool CanRenew(Prescription o, PrescriptionsAccessSubject s) => s.CanRenew && (s.CanManageAll || (s.UserId.HasValue && o.AuthorUserId == s.UserId.Value));
    public bool CanCancel(Prescription p, PrescriptionsAccessSubject s) => s.CanCancel && (s.CanManageAll || (s.UserId.HasValue && p.AuthorUserId == s.UserId.Value));
    public bool CanDispense(Prescription p, PrescriptionsAccessSubject s) => s.CanDispense && s.UserId.HasValue;
    public bool CanExpire(Prescription p, PrescriptionsAccessSubject s) => s.CanExpire && s.UserId.HasValue;
    public bool CanReadDocuments(Prescription p, PrescriptionsAccessSubject s) => s.CanReadDocuments && (s.CanManageAll || (s.UserId.HasValue && p.AuthorUserId == s.UserId.Value));
    public bool CanGenerateDocuments(Prescription p, PrescriptionsAccessSubject s) => (s.CanManageDocuments || s.CanManageAll) && (s.CanManageAll || (s.UserId.HasValue && p.AuthorUserId == s.UserId.Value));
}
