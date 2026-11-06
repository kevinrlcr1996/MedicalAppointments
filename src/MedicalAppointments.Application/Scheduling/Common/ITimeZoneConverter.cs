namespace MedicalAppointments.Application.Scheduling.Common;

public interface ITimeZoneConverter
{
    bool TryConvertLocalToUtc(
        DateOnly date,
        TimeOnly time,
        string? timeZoneId,
        out DateTimeOffset utc);

    TimeRange GetLocalDayRangeUtc(
        DateOnly date,
        string? timeZoneId);
}