using MedicalAppointments.Domain.Common;
using MedicalAppointments.Domain.Scheduling;

namespace MedicalAppointments.UnitTests.Domain;

public class ScheduleExceptionTests
{
    [Fact]
    public void Create_Should_Throw_When_MultiDay_With_Times()
    {
        var now = DateTimeOffset.UtcNow;

        Assert.Throws<DomainException>(() =>
            ScheduleException.Create(
                Guid.NewGuid(),
                Guid.NewGuid(),
                DateOnly.FromDateTime(now.DateTime),
                DateOnly.FromDateTime(now.DateTime).AddDays(2),
                new TimeOnly(9, 0),
                new TimeOnly(17, 0),
                ScheduleExceptionType.Vacation,
                null,
                true,
                now));
    }
}