namespace MedicalAppointments.Domain.WaitingRoom;

public enum WaitingRoomCallStatus
{
    Queued = 0,
    Called = 1,
    InConsultation = 2,
    Skipped = 3,
    Completed = 4
}