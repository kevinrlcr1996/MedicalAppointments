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

    public const string WaitingListOfferCreated = "WaitingListOfferCreated";
    public const string WaitingListUpdated = "WaitingListUpdated";

    public const string WaitingRoomUpdated = "WaitingRoomUpdated";
    public const string PatientCalled = "PatientCalled";
    public const string PatientRecalled = "PatientRecalled";

    public const string ClinicalNoteUpdated = "ClinicalNoteUpdated";
}