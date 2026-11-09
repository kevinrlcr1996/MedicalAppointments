using System.Security.Claims;
using MedicalAppointments.Application.Authorization;
using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.MedicalStructure.Doctors;
using MedicalAppointments.Domain.Appointments;
using MedicalAppointments.Domain.ClinicalRecords;

namespace MedicalAppointments.Application.ClinicalRecords.Auth;

public sealed class ClinicalRecordsAccessService : IClinicalRecordsAccessService
{
    private readonly ICurrentPrincipalProvider _principalProvider;
    private readonly IDoctorStore _doctorStore;

    public ClinicalRecordsAccessService(
        ICurrentPrincipalProvider principalProvider,
        IDoctorStore doctorStore)
    {
        _principalProvider = principalProvider;
        _doctorStore = doctorStore;
    }

    public async Task<ClinicalRecordsAccessSubject> GetSubjectAsync(CancellationToken cancellationToken = default)
    {
        var principal = _principalProvider.GetCurrentPrincipal()
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var userId = Guid.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var parsedUserId)
            ? parsedUserId
            : (Guid?)null;

        var hasRead = principal.HasPermission(Permissions.ClinicalRecordsRead);

        // El alcance de lectura se delega en los permisos de CITAS (read_all / read_assigned),
        // combinados con clinical_records.read. El paciente no tiene clinical_records.read en Fase 8.
        var canReadAll = hasRead && principal.HasPermission(Permissions.AppointmentsReadAll);
        var canReadAssigned = hasRead && principal.HasPermission(Permissions.AppointmentsReadAssigned);

        var canCreate = principal.HasPermission(Permissions.ClinicalRecordsCreate);
        var canUpdate = principal.HasPermission(Permissions.ClinicalRecordsUpdate);
        var canFinalize = principal.HasPermission(Permissions.ClinicalRecordsFinalize);
        var canCorrect = principal.HasPermission(Permissions.ClinicalRecordsCorrect);
        var canManageAll = principal.HasPermission(Permissions.ClinicalRecordsManageAll);

        Guid? doctorId = null;

        var needsDoctorId = canReadAssigned || canCreate;

        if (userId.HasValue && needsDoctorId)
        {
            doctorId = await _doctorStore.GetIdByUserIdAsync(userId.Value, cancellationToken);
        }

        return new ClinicalRecordsAccessSubject(
            UserId: userId,
            DoctorId: doctorId,
            CanReadAll: canReadAll,
            CanReadAssigned: canReadAssigned,
            CanCreate: canCreate,
            CanUpdate: canUpdate,
            CanFinalize: canFinalize,
            CanCorrect: canCorrect,
            CanManageAll: canManageAll);
    }

    public bool CanReadNotes(ClinicalRecordsAccessSubject subject)
    {
        return subject.CanReadAll || subject.CanReadAssigned;
    }

    public bool CanReadNotesForAppointment(Appointment appointment, ClinicalRecordsAccessSubject subject)
    {
        return subject.CanReadAll
            || (subject.CanReadAssigned && subject.DoctorId == appointment.DoctorId);
    }

    public bool CanCreateForAppointment(Appointment appointment, ClinicalRecordsAccessSubject subject)
    {
        // Solo un profesional con perfil médico, y solo sobre su propia cita (o manage_all).
        return subject.CanCreate
            && subject.DoctorId.HasValue
            && (subject.CanManageAll || subject.DoctorId.Value == appointment.DoctorId);
    }

    public bool CanUpdateNote(ClinicalNote note, ClinicalRecordsAccessSubject subject)
    {
        return subject.CanUpdate
            && (subject.CanManageAll || (subject.UserId.HasValue && note.AuthorUserId == subject.UserId.Value));
    }

    public bool CanFinalizeNote(ClinicalNote note, ClinicalRecordsAccessSubject subject)
    {
        return subject.CanFinalize
            && (subject.CanManageAll || (subject.UserId.HasValue && note.AuthorUserId == subject.UserId.Value));
    }

    public bool CanCorrectNote(ClinicalNote original, ClinicalRecordsAccessSubject subject)
    {
        return subject.CanCorrect
            && (subject.CanManageAll || (subject.UserId.HasValue && original.AuthorUserId == subject.UserId.Value));
    }
}