using MedicalAppointments.Domain.Outbox;

namespace MedicalAppointments.UnitTests.Domain;

public class OutboxMessageTests
{
    [Fact]
    public void Create_Should_Start_Pending()
    {
        var now = DateTimeOffset.UtcNow;

        var message = OutboxMessage.Create(
            type: "Test.Type",
            payload: "{}",
            occurredAt: now,
            createdAt: now);

        Assert.Equal(OutboxStatus.Pending, message.Status);
        Assert.Equal(0, message.Attempts);
        Assert.Equal(now, message.NextAttemptAt);
    }

    [Fact]
    public void ScheduleRetry_Should_Increase_Attempts_And_Set_Backoff()
    {
        var now = DateTimeOffset.UtcNow;

        var message = OutboxMessage.Create("Test.Type", "{}", now, now);
        message.MarkProcessing("owner", now);

        message.ScheduleRetry(
            now,
            "error",
            maxAttempts: 5,
            minimumBackoff: TimeSpan.FromSeconds(30),
            maximumBackoff: TimeSpan.FromMinutes(10));

        Assert.Equal(OutboxStatus.Pending, message.Status);
        Assert.Equal(1, message.Attempts);
        Assert.NotNull(message.NextAttemptAt);
        Assert.True(message.NextAttemptAt > now);
    }

    [Fact]
    public void ScheduleRetry_Should_Fail_After_Max_Attempts()
    {
        var now = DateTimeOffset.UtcNow;

        var message = OutboxMessage.Create("Test.Type", "{}", now, now);
        message.MarkProcessing("owner", now);

        message.ScheduleRetry(now, "error", maxAttempts: 1, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10));

        Assert.Equal(OutboxStatus.Failed, message.Status);
        Assert.Null(message.NextAttemptAt);
    }
}