using MedicalAppointments.Domain.Appointments;

namespace MedicalAppointments.Application.Appointments.Auth;

public interface IAppointmentAccessService
{
    Task<AppointmentAccessSubject> GetSubjectAsync(CancellationToken cancellationToken = default);

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

    bool CanRead(Appointment appointment, AppointmentAccessSubject subject);

    bool CanCreate(
        CreateAppointmentCommand command,
        AppointmentAccessSubject subject);

    bool CanCancel(Appointment appointment, AppointmentAccessSubject subject);

    bool CanReschedule(Appointment appointment, AppointmentAccessSubject subject);

    bool CanConfirm(Appointment appointment, AppointmentAccessSubject subject);

    bool CanCheckIn(Appointment appointment, AppointmentAccessSubject subject);

    bool CanStart(Appointment appointment, AppointmentAccessSubject subject);

    bool CanComplete(Appointment appointment, AppointmentAccessSubject subject);

    bool CanMarkNoShow(Appointment appointment, AppointmentAccessSubject subject);
}