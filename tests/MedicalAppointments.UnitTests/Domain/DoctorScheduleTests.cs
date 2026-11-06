using MedicalAppointments.Domain.Common;
using MedicalAppointments.Domain.Scheduling;

namespace MedicalAppointments.UnitTests.Domain;

public class DoctorScheduleTests
{
    [Fact]
    public void ReplaceDays_Should_Throw_When_Duplicate_Day()
    {
        var now = DateTimeOffset.UtcNow;

        var schedule = DoctorSchedule.Create(
            doctorId: Guid.NewGuid(),
            clinicId: Guid.NewGuid(),
            name: "Test",
            isDefault: false,
            isActive: true,
            effectiveFrom: DateOnly.FromDateTime(now.DateTime),
            effectiveTo: null,
            slotDurationMinutes: 30,
            bufferMinutes: 0,
            days: [new DoctorScheduleDayInput(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0))],
            now: now);

        Assert.Throws<DomainException>(() =>
            schedule.ReplaceDays(
                [
                    new DoctorScheduleDayInput(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0)),
                    new DoctorScheduleDayInput(DayOfWeek.Monday, new TimeOnly(10, 0), new TimeOnly(18, 0))
                ],
                now));
    }

    [Fact]
    public void ReplaceDays_Should_Update_Existing_Days_Without_Replacing_Entities()
    {
        var now = DateTimeOffset.UtcNow;
        var schedule = DoctorSchedule.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test",
            false,
            true,
            DateOnly.FromDateTime(now.DateTime),
            null,
            30,
            0,
            [
                new DoctorScheduleDayInput(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0)),
                new DoctorScheduleDayInput(DayOfWeek.Tuesday, new TimeOnly(9, 0), new TimeOnly(17, 0))
            ],
            now);

        var monday = schedule.Days.Single(x => x.DayOfWeek == DayOfWeek.Monday);

        schedule.ReplaceDays(
            [
                new DoctorScheduleDayInput(DayOfWeek.Monday, new TimeOnly(10, 0), new TimeOnly(18, 0)),
                new DoctorScheduleDayInput(DayOfWeek.Wednesday, new TimeOnly(8, 0), new TimeOnly(16, 0))
            ],
            now.AddMinutes(1));

        var updatedMonday = schedule.Days.Single(x => x.DayOfWeek == DayOfWeek.Monday);

        Assert.Same(monday, updatedMonday);
        Assert.Equal(new TimeOnly(10, 0), updatedMonday.StartTime);
        Assert.Equal(new TimeOnly(18, 0), updatedMonday.EndTime);
        Assert.DoesNotContain(schedule.Days, x => x.DayOfWeek == DayOfWeek.Tuesday);
        Assert.Contains(schedule.Days, x => x.DayOfWeek == DayOfWeek.Wednesday);
    }

    [Fact]
    public void AppliesToDate_Should_Respect_Effective_Range()
    {
        var now = DateTimeOffset.UtcNow;
        var from = DateOnly.FromDateTime(now.DateTime).AddDays(1);
        var to = from.AddDays(5);

        var schedule = DoctorSchedule.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test",
            false,
            true,
            from,
            to,
            30,
            0,
            [new DoctorScheduleDayInput(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0))],
            now);

        Assert.False(schedule.AppliesToDate(from.AddDays(-1)));
        Assert.True(schedule.AppliesToDate(from));
        Assert.True(schedule.AppliesToDate(to));
        Assert.False(schedule.AppliesToDate(to.AddDays(1)));
    }
}