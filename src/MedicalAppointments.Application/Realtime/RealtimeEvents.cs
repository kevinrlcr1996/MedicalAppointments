namespace MedicalAppointments.Application.Realtime;

public static class RealtimeEvents
{
    public const string TestEvent = "TestEvent";
    public const string ScheduleChanged = "ScheduleChanged";
    public const string AvailabilityChanged = "AvailabilityChanged";

    public const string AppointmentCreated = "AppointmentCreated";
    public const string AppointmentCancelled = "AppointmentCancelled";
    public const string AppointmentRescheduled = "AppointmentRescheduled";
    public const string AppointmentStatusChanged = "AppointmentStatusChanged";
}