namespace MedicalAppointments.Domain.Scheduling;

public sealed class DoctorScheduleDay
{
    private DoctorScheduleDay()
    {
    }

    public DoctorScheduleDay(
        Guid id,
        Guid doctorScheduleId,
        DayOfWeek dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        DateTimeOffset createdAt)
    {
        Id = id;
        DoctorScheduleId = doctorScheduleId;
        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid DoctorScheduleId { get; private set; }

    public DayOfWeek DayOfWeek { get; private set; }

    public TimeOnly StartTime { get; private set; }

    public TimeOnly EndTime { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DoctorSchedule DoctorSchedule { get; private set; } = null!;

    public void Update(TimeOnly startTime, TimeOnly endTime)
    {
        StartTime = startTime;
        EndTime = endTime;
    }
}