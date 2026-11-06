using MedicalAppointments.Application.Scheduling.Common;

namespace MedicalAppointments.Infrastructure.Scheduling.Time;

public sealed class TimeZoneInfoConverter : ITimeZoneConverter
{
    public bool TryConvertLocalToUtc(
        DateOnly date,
        TimeOnly time,
        string? timeZoneId,
        out DateTimeOffset utc)
    {
        utc = default;

        var tz = ResolveTimeZone(timeZoneId);

        var local = new DateTime(
            date.Year,
            date.Month,
            date.Day,
            time.Hour,
            time.Minute,
            0,
            DateTimeKind.Unspecified);

        if (tz.IsInvalidTime(local))
        {
            return false;
        }

        if (tz.IsAmbiguousTime(local))
        {
            return false;
        }

        try
        {
            var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(local, tz);
            utc = new DateTimeOffset(utcDateTime, TimeSpan.Zero);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public TimeRange GetLocalDayRangeUtc(
        DateOnly date,
        string? timeZoneId)
    {
        if (TryConvertLocalToUtc(date, new TimeOnly(0, 0), timeZoneId, out var start) &&
            TryConvertLocalToUtc(date.AddDays(1), new TimeOnly(0, 0), timeZoneId, out var end))
        {
            return new TimeRange(start, end);
        }

        var fallbackStart = new DateTimeOffset(date.ToDateTime(new TimeOnly(0, 0)), TimeSpan.Zero);
        var fallbackEnd = fallbackStart.AddDays(1);

        return new TimeRange(fallbackStart, fallbackEnd);
    }

    private static TimeZoneInfo ResolveTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return TimeZoneInfo.Utc;
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (Exception)
        {
            return TimeZoneInfo.Utc;
        }
    }
}