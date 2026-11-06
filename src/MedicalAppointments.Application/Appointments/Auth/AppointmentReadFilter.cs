namespace MedicalAppointments.Application.Appointments.Auth;

public sealed record AppointmentReadFilter(
    bool All,
    IReadOnlyCollection<Guid> DoctorIds,
    IReadOnlyCollection<Guid> PatientIds,
    bool Denied)
{
    // Denied = el sujeto NO tiene ningún permiso de lectura. Eso sí es 403.
    // Scope vacío (Denied=false, listas vacías) NO es 403: el store devuelve [].
    public static AppointmentReadFilter Deny() =>
        new(false, Array.Empty<Guid>(), Array.Empty<Guid>(), true);

    public static AppointmentReadFilter AllAccess() =>
        new(true, Array.Empty<Guid>(), Array.Empty<Guid>(), false);

    public static AppointmentReadFilter Scoped(
        IReadOnlyCollection<Guid> doctorIds,
        IReadOnlyCollection<Guid> patientIds) =>
        new(false, doctorIds, patientIds, false);
}