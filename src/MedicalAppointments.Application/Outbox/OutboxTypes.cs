namespace MedicalAppointments.Application.Outbox;

public static class OutboxTypes
{
    public const string FoundationTestEvent = "Foundation.TestEvent";
    public const string ScheduleChanged = "Scheduling.ScheduleChanged";
    public const string AvailabilityChanged = "Scheduling.AvailabilityChanged";

    public const string AppointmentCreated = "Appointments.Created";
    public const string AppointmentCancelled = "Appointments.Cancelled";
    public const string AppointmentRescheduled = "Appointments.Rescheduled";
    public const string AppointmentStatusChanged = "Appointments.StatusChanged";
}