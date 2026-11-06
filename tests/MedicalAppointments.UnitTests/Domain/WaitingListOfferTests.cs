using MedicalAppointments.Domain.Common;
using MedicalAppointments.Domain.WaitingList;

namespace MedicalAppointments.UnitTests.Domain;

public class WaitingListOfferTests
{
    [Fact]
    public void MarkAccepted_Should_Throw_When_Expired()
    {
        var now = DateTimeOffset.UtcNow;
        var offer = WaitingListOffer.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null,
            DateOnly.FromDateTime(now.DateTime), now.AddHours(1), now.AddHours(1).AddMinutes(30),
            now.AddMinutes(5), now);

        Assert.Throws<DomainException>(() => offer.MarkAccepted(now.AddMinutes(10)));
    }
}