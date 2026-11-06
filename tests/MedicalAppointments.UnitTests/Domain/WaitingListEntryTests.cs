using MedicalAppointments.Domain.Common;
using MedicalAppointments.Domain.WaitingList;
using Xunit;

namespace MedicalAppointments.UnitTests.Domain;

public class WaitingListEntryTests
{
    [Fact]
    public void MatchesSlot_Should_Respect_Window_And_Doctor()
    {
        var now = DateTimeOffset.UtcNow;
        var doctorId = Guid.NewGuid();
        var clinicId = Guid.NewGuid();

        var entry = WaitingListEntry.Create(
            Guid.NewGuid(), doctorId, clinicId,
            now.AddDays(-1), now.AddDays(7),
            WaitingListPriority.Normal, null, now);

        Assert.True(entry.MatchesSlot(now.AddDays(1), now.AddDays(1).AddMinutes(30), doctorId, clinicId));
        Assert.False(entry.MatchesSlot(now.AddDays(1), now.AddDays(1).AddMinutes(30), Guid.NewGuid(), clinicId));
        Assert.False(entry.MatchesSlot(now.AddDays(30), now.AddDays(30).AddMinutes(30), doctorId, clinicId));
    }

    [Fact]
    public void Cancel_Should_Throw_When_Fulfilled()
    {
        var now = DateTimeOffset.UtcNow;
        var entry = WaitingListEntry.Create(
            Guid.NewGuid(), Guid.NewGuid(), null, now, now.AddDays(1),
            WaitingListPriority.Normal, null, now);

        entry.MarkFulfilled(now);

        Assert.Throws<DomainException>(() => entry.Cancel(now));
    }
}