namespace MedicalAppointments.Application.Scheduling.Outbox;

public interface ISchedulingEventNotifier
{
    void EnqueueScheduleChanged(
        Guid scheduleId,
        Guid doctorId,
        Guid clinicId,
        string changeType,
        DateOnly affectedFrom,
        DateOnly affectedTo);

    void EnqueueAvailabilityChanged(
        Guid doctorId,
        Guid clinicId,
        DateOnly affectedFrom,
        DateOnly affectedTo);
}