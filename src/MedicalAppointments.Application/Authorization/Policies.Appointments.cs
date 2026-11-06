namespace MedicalAppointments.Application.Authorization;

public static partial class Policies
{
    public const string AppointmentsRead = nameof(AppointmentsRead);
    public const string AppointmentsCreate = nameof(AppointmentsCreate);
    public const string AppointmentsCancel = nameof(AppointmentsCancel);
    public const string AppointmentsReschedule = nameof(AppointmentsReschedule);
    public const string AppointmentsCheckIn = nameof(AppointmentsCheckIn);
    public const string AppointmentsComplete = nameof(AppointmentsComplete);
    public const string AppointmentsMarkNoShow = nameof(AppointmentsMarkNoShow);
}