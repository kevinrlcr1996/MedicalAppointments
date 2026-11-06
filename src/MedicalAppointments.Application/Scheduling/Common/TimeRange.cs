namespace MedicalAppointments.Application.Scheduling.Common;

public readonly record struct TimeRange(
    DateTimeOffset StartAt,
    DateTimeOffset EndAt)
{
    public bool IsValid => EndAt > StartAt;

    public bool Overlaps(TimeRange other)
    {
        return StartAt < other.EndAt && EndAt > other.StartAt;
    }
}