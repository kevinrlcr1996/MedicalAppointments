using MedicalAppointments.Domain.Common;
using MedicalAppointments.Domain.WaitingRoom;
using Xunit;

namespace MedicalAppointments.UnitTests.Domain;

public class WaitingRoomCallTests
{
    [Fact]
    public void CallNext_Flow_Should_Transition_Queued_Called_InConsultation_Completed()
    {
        var now = DateTimeOffset.UtcNow;
        var call = WaitingRoomCall.Queue(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateOnly.FromDateTime(now.DateTime), 1, null, now);

        Assert.Equal(WaitingRoomCallStatus.Queued, call.Status);

        call.MarkCalled(Guid.NewGuid(), now);
        Assert.Equal(WaitingRoomCallStatus.Called, call.Status);

        call.MarkRecalled(now);
        Assert.Equal(1, call.RecallCount);
        Assert.Equal(WaitingRoomCallStatus.Called, call.Status);

        call.MarkInConsultation(now);
        Assert.Equal(WaitingRoomCallStatus.InConsultation, call.Status);

        call.MarkCompleted(now);
        Assert.Equal(WaitingRoomCallStatus.Completed, call.Status);
    }

    [Fact]
    public void MarkInConsultation_Should_Throw_When_Not_Called()
    {
        var now = DateTimeOffset.UtcNow;
        var call = WaitingRoomCall.Queue(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateOnly.FromDateTime(now.DateTime), 1, null, now);
        Assert.Throws<DomainException>(() => call.MarkInConsultation(now));
    }

    [Fact]
    public void Recall_From_Skipped_Should_Return_To_Called_And_Increment_RecallCount()
    {
        var now = DateTimeOffset.UtcNow;
        var call = WaitingRoomCall.Queue(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateOnly.FromDateTime(now.DateTime), 1, null, now);

        call.MarkCalled(Guid.NewGuid(), now);
        call.MarkSkipped(now);
        Assert.Equal(WaitingRoomCallStatus.Skipped, call.Status);

        // FIX: recall unificado — un Skipped vuelve a Called con RecallCount++.
        call.MarkRecalled(now);
        Assert.Equal(WaitingRoomCallStatus.Called, call.Status);
        Assert.Equal(1, call.RecallCount);
    }

    [Fact]
    public void Recall_Should_Throw_From_Queued_Or_Completed()
    {
        var now = DateTimeOffset.UtcNow;

        var queued = WaitingRoomCall.Queue(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateOnly.FromDateTime(now.DateTime), 1, null, now);
        Assert.Throws<DomainException>(() => queued.MarkRecalled(now));

        var completed = WaitingRoomCall.Queue(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateOnly.FromDateTime(now.DateTime), 2, null, now);
        completed.MarkCalled(Guid.NewGuid(), now);
        completed.MarkInConsultation(now);
        completed.MarkCompleted(now);
        Assert.Throws<DomainException>(() => completed.MarkRecalled(now));
    }
}