using System.Security.Claims;
using MedicalAppointments.Application.Authorization;
using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.MedicalStructure.Doctors;
using MedicalAppointments.Application.MedicalStructure.Patients;
using MedicalAppointments.Domain.Appointments;

namespace MedicalAppointments.Application.Appointments.Auth;

public sealed class AppointmentAccessService : IAppointmentAccessService
{
    private readonly ICurrentPrincipalProvider _principalProvider;
    private readonly IPatientStore _patientStore;
    private readonly IDoctorStore _doctorStore;

    public AppointmentAccessService(
        ICurrentPrincipalProvider principalProvider,
        IPatientStore patientStore,
        IDoctorStore doctorStore)
    {
        _principalProvider = principalProvider;
        _patientStore = patientStore;
        _doctorStore = doctorStore;
    }

    public async Task<AppointmentAccessSubject> GetSubjectAsync(CancellationToken cancellationToken = default)
    {
        var principal = _principalProvider.GetCurrentPrincipal()
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var userId = Guid.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var parsedUserId)
            ? parsedUserId
            : (Guid?)null;

        var canReadAll = principal.HasPermission(Permissions.AppointmentsReadAll);
        var canReadAssigned = principal.HasPermission(Permissions.AppointmentsReadAssigned);
        var canReadOwn = principal.HasPermission(Permissions.AppointmentsReadOwn);

        var canCreateAll = principal.HasPermission(Permissions.AppointmentsCreateAll);
        var canCreateAssigned = principal.HasPermission(Permissions.AppointmentsCreateAssigned);
        var canCreateOwn = principal.HasPermission(Permissions.AppointmentsCreateOwn);

        var canUpdateAll = principal.HasPermission(Permissions.AppointmentsUpdateAll);
        var canUpdateAssigned = principal.HasPermission(Permissions.AppointmentsUpdateAssigned);

        var canCancelAll = principal.HasPermission(Permissions.AppointmentsCancelAll);
        var canCancelAssigned = principal.HasPermission(Permissions.AppointmentsCancelAssigned);
        var canCancelOwn = principal.HasPermission(Permissions.AppointmentsCancelOwn);

        var canRescheduleAll = principal.HasPermission(Permissions.AppointmentsRescheduleAll);
        var canRescheduleAssigned = principal.HasPermission(Permissions.AppointmentsRescheduleAssigned);
        var canRescheduleOwn = principal.HasPermission(Permissions.AppointmentsRescheduleOwn);

        var canCheckIn = principal.HasPermission(Permissions.AppointmentsCheckIn);
        var canComplete = principal.HasPermission(Permissions.AppointmentsComplete);
        var canMarkNoShow = principal.HasPermission(Permissions.AppointmentsMarkNoShow);
        var canOverridePolicy = principal.HasPermission(Permissions.AppointmentsOverridePolicy);

        var needsDoctorId =
            canReadAssigned ||
            canCreateAssigned ||
            canUpdateAssigned ||
            canCancelAssigned ||
            canRescheduleAssigned ||
            canCheckIn ||
            canComplete ||
            canMarkNoShow;

        var needsPatientId =
            canReadOwn ||
            canCreateOwn ||
            canCancelOwn ||
            canRescheduleOwn;

        Guid? doctorId = null;
        Guid? patientId = null;

        if (userId.HasValue && needsDoctorId)
        {
            doctorId = await _doctorStore.GetIdByUserIdAsync(userId.Value, cancellationToken);
        }

        if (userId.HasValue && needsPatientId)
        {
            patientId = await _patientStore.GetIdByUserIdAsync(userId.Value, cancellationToken);
        }

        var canManageAll = canUpdateAll || canCancelAll || canRescheduleAll;
        var canManageAssigned = canUpdateAssigned || canCancelAssigned || canRescheduleAssigned;

        return new AppointmentAccessSubject(
            UserId: userId,
            PatientId: patientId,
            DoctorId: doctorId,
            CanReadAll: canReadAll,
            CanReadAssigned: canReadAssigned,
            CanReadOwn: canReadOwn,
            CanCreateAll: canCreateAll,
            CanCreateAssigned: canCreateAssigned,
            CanCreateOwn: canCreateOwn,
            CanUpdateAll: canUpdateAll,
            CanUpdateAssigned: canUpdateAssigned,
            CanCancelAll: canCancelAll,
            CanCancelAssigned: canCancelAssigned,
            CanCancelOwn: canCancelOwn,
            CanRescheduleAll: canRescheduleAll,
            CanRescheduleAssigned: canRescheduleAssigned,
            CanRescheduleOwn: canRescheduleOwn,
            CanCheckIn: canCheckIn,
            CanComplete: canComplete,
            CanMarkNoShow: canMarkNoShow,
            CanManageAll: canManageAll,
            CanManageAssigned: canManageAssigned,
            CanOverridePolicy: canOverridePolicy);
    }

    public AppointmentReadFilter BuildReadFilter(AppointmentAccessSubject subject)
    {
        if (subject.CanReadAll)
        {
            return AppointmentReadFilter.AllAccess();
        }

        // Si no tiene NINGÚN permiso de lectura, es 403 real.
        var canReadAny = subject.CanReadAssigned || subject.CanReadOwn;

        if (!canReadAny)
        {
            return AppointmentReadFilter.Deny();
        }

        var doctorIds = new List<Guid>();
        var patientIds = new List<Guid>();

        if (subject.CanReadAssigned && subject.DoctorId.HasValue)
        {
            doctorIds.Add(subject.DoctorId.Value);
        }

        if (subject.CanReadOwn && subject.PatientId.HasValue)
        {
            patientIds.Add(subject.PatientId.Value);
        }

        // Puede estar vacío (paciente sin ficha / doctor sin perfil).
        // Eso NO es Denied: el store lo resuelve devolviendo [].
        return AppointmentReadFilter.Scoped(doctorIds, patientIds);
    }

    public bool CanRead(Appointment appointment, AppointmentAccessSubject subject)
    {
        return subject.CanReadAll
            || (subject.CanReadAssigned && subject.DoctorId == appointment.DoctorId)
            || (subject.CanReadOwn && subject.PatientId == appointment.PatientId);
    }

    public bool CanCreate(CreateAppointmentCommand command, AppointmentAccessSubject subject)
    {
        return subject.CanCreateAll
            || (subject.CanCreateAssigned && subject.DoctorId == command.DoctorId)
            || (subject.CanCreateOwn && subject.PatientId == command.PatientId);
    }

    public bool CanCancel(Appointment appointment, AppointmentAccessSubject subject)
    {
        return subject.CanCancelAll
            || (subject.CanCancelAssigned && subject.DoctorId == appointment.DoctorId)
            || (subject.CanCancelOwn && subject.PatientId == appointment.PatientId);
    }

    public bool CanReschedule(Appointment appointment, AppointmentAccessSubject subject)
    {
        return subject.CanRescheduleAll
            || (subject.CanRescheduleAssigned && subject.DoctorId == appointment.DoctorId)
            || (subject.CanRescheduleOwn && subject.PatientId == appointment.PatientId);
    }

    public bool CanConfirm(Appointment appointment, AppointmentAccessSubject subject)
    {
        return subject.CanManageAll
            || (subject.CanManageAssigned && subject.DoctorId == appointment.DoctorId);
    }

    public bool CanCheckIn(Appointment appointment, AppointmentAccessSubject subject)
    {
        return subject.CanCheckIn &&
            (subject.CanManageAll ||
             (subject.CanManageAssigned && subject.DoctorId == appointment.DoctorId));
    }

    public bool CanStart(Appointment appointment, AppointmentAccessSubject subject)
    {
        return CanCheckIn(appointment, subject);
    }

    public bool CanComplete(Appointment appointment, AppointmentAccessSubject subject)
    {
        return subject.CanComplete &&
            (subject.CanManageAll ||
             (subject.CanManageAssigned && subject.DoctorId == appointment.DoctorId));
    }

    public bool CanMarkNoShow(Appointment appointment, AppointmentAccessSubject subject)
    {
        return subject.CanMarkNoShow &&
            (subject.CanManageAll ||
             (subject.CanManageAssigned && subject.DoctorId == appointment.DoctorId));
    }
}