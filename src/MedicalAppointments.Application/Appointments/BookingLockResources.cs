namespace MedicalAppointments.Application.Appointments;

public static class BookingLockResources
{
    public static IEnumerable<string> ForInterval(
        Guid doctorId,
        Guid clinicId,
        Guid? roomId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc)
    {
        if (endUtc <= startUtc)
        {
            yield break;
        }

        var firstDate = startUtc.UtcDateTime.Date;
        var lastDate = endUtc.UtcDateTime.AddTicks(-1).Date;

        for (var date = firstDate; date <= lastDate; date = date.AddDays(1))
        {
            var key = date.ToString("yyyy-MM-dd");

            yield return $"booking:doctor:{doctorId}:clinic:{clinicId}:date:{key}";

            if (roomId.HasValue)
            {
                yield return $"booking:room:{roomId.Value}:date:{key}";
            }
        }
    }
}