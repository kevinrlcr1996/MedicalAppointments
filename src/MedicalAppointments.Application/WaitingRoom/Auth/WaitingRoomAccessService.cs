using MedicalAppointments.Application.Appointments.Auth;
using MedicalAppointments.Application.Authorization;
using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.MedicalStructure.Doctors;
using MedicalAppointments.Domain.Appointments;
using System.Security.Claims;

namespace MedicalAppointments.Application.WaitingRoom.Auth;

public sealed class WaitingRoomAccessService : IWaitingRoomAccessService
{
    private readonly ICurrentPrincipalProvider _principalProvider;
    private readonly IDoctorStore _doctorStore;
    private readonly IAppointmentAccessService _appointmentAccess;

    public WaitingRoomAccessService(
        ICurrentPrincipalProvider principalProvider,
        IDoctorStore doctorStore,
        IAppointmentAccessService appointmentAccess)
    {
        _principalProvider = principalProvider;
        _doctorStore = doctorStore;
        _appointmentAccess = appointmentAccess;
    }

    public async Task<WaitingRoomAccessSubject> GetSubjectAsync(CancellationToken cancellationToken = default)
    {
        var principal = _principalProvider.GetCurrentPrincipal()
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var userId = Guid.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var parsedUserId)
            ? parsedUserId
            : (Guid?)null;

        var canReadAll = principal.HasPermission(Permissions.WaitingRoomRead);
        var canManageAll = principal.HasPermission(Permissions.WaitingRoomManage);

        var canOperateAssigned =
            principal.HasPermission(Permissions.AppointmentsCheckIn) ||
            principal.HasPermission(Permissions.AppointmentsComplete) ||
            principal.HasPermission(Permissions.AppointmentsMarkNoShow);

        var canReadAssigned = principal.HasPermission(Permissions.AppointmentsReadAssigned);

        Guid? doctorId = null;

        var needsDoctorId = canOperateAssigned || canReadAssigned;

        if (userId.HasValue && needsDoctorId)
        {
            doctorId = await _doctorStore.GetIdByUserIdAsync(userId.Value, cancellationToken);
        }

        return new WaitingRoomAccessSubject(
            UserId: userId,
            DoctorId: doctorId,
            CanReadAll: canReadAll,
            CanManageAll: canManageAll,
            CanOperateAssigned: canOperateAssigned,
            CanReadAssigned: canReadAssigned);
    }

    public bool CanViewBoard(Guid doctorId, WaitingRoomAccessSubject subject)
    {
        return subject.CanReadAll
            || subject.CanManageAll
            || (subject.CanReadAssigned && subject.DoctorId == doctorId);
    }

    public bool CanOperateQueue(Guid doctorId, WaitingRoomAccessSubject subject)
    {
        return subject.CanManageAll
            || (subject.CanOperateAssigned && subject.DoctorId == doctorId);
    }

    public bool CanAnswer(Appointment appointment, WaitingRoomAccessSubject subject)
    {
        if (subject.CanManageAll)
        {
            return true;
        }

        return _appointmentAccess.CanStart(appointment, BuildAppointmentSubject(subject));
    }

    public bool CanComplete(Appointment appointment, WaitingRoomAccessSubject subject)
    {
        if (subject.CanManageAll)
        {
            return true;
        }

        return _appointmentAccess.CanComplete(appointment, BuildAppointmentSubject(subject));
    }

    public bool CanMarkNoShow(Appointment appointment, WaitingRoomAccessSubject subject)
    {
        if (subject.CanManageAll)
        {
            return true;
        }

        return _appointmentAccess.CanMarkNoShow(appointment, BuildAppointmentSubject(subject));
    }

    // Reconstruye el sujeto de citas a partir del de sala para delegar sin recargar claims/BD.
    // Solo se usa en rutas no-global; por eso DoctorId viene del propio sujeto.
    // No necesita la cita: las comparaciones de IAppointmentAccessService usan subject.DoctorId vs appointment.DoctorId.
    private static AppointmentAccessSubject BuildAppointmentSubject(WaitingRoomAccessSubject subject)
    {
        return new AppointmentAccessSubject(
            UserId: subject.UserId,
            PatientId: null,
            DoctorId: subject.DoctorId,
            CanReadAll: false,
            CanReadAssigned: subject.CanReadAssigned,
            CanReadOwn: false,
            CanCreateAll: false,
            CanCreateAssigned: false,
            CanCreateOwn: false,
            CanUpdateAll: false,
            CanUpdateAssigned: subject.CanOperateAssigned,
            CanCancelAll: false,
            CanCancelAssigned: subject.CanOperateAssigned,
            CanCancelOwn: false,
            CanRescheduleAll: false,
            CanRescheduleAssigned: false,
            CanRescheduleOwn: false,
            CanCheckIn: subject.CanOperateAssigned,
            CanComplete: subject.CanOperateAssigned,
            CanMarkNoShow: subject.CanOperateAssigned,
            CanManageAll: false,
            CanManageAssigned: subject.CanOperateAssigned,
            CanOverridePolicy: false);
    }
}