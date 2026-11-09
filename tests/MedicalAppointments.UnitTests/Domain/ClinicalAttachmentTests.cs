using MedicalAppointments.Domain.ClinicalRecords;
using MedicalAppointments.Domain.Common;
using Xunit;

namespace MedicalAppointments.UnitTests.Domain;

public class ClinicalAttachmentTests
{
    private static string FakeHash() => new string('a', 64);

    [Fact]
    public void Create_Should_Persist_Clean_Or_Skipped_Status()
    {
        var now = DateTimeOffset.UtcNow;
        var att = ClinicalAttachment.Create(
            Guid.NewGuid(), "lab.pdf", "application/pdf", 1024,
            "notes/x/y.pdf", FakeHash(), ClinicalAttachmentScanStatus.Clean, now, Guid.NewGuid(), now);

        Assert.Equal(ClinicalAttachmentScanStatus.Clean, att.ScanStatus);
        Assert.Equal(1024, att.SizeBytes);
    }

    [Fact]
    public void Create_Should_Throw_On_Bad_Hash_Length()
    {
        var now = DateTimeOffset.UtcNow;
        Assert.Throws<DomainException>(() => ClinicalAttachment.Create(
            Guid.NewGuid(), "lab.pdf", "application/pdf", 1024,
            "notes/x/y.pdf", "tooshort", ClinicalAttachmentScanStatus.Clean, now, Guid.NewGuid(), now));
    }

    [Fact]
    public void Create_Should_Throw_On_Zero_Size()
    {
        var now = DateTimeOffset.UtcNow;
        Assert.Throws<DomainException>(() => ClinicalAttachment.Create(
            Guid.NewGuid(), "lab.pdf", "application/pdf", 0,
            "notes/x/y.pdf", FakeHash(), ClinicalAttachmentScanStatus.Clean, now, Guid.NewGuid(), now));
    }

    [Fact]
    public void SoftDelete_Should_Mark_Flags_And_Touch()
    {
        var now = DateTimeOffset.UtcNow;
        var att = ClinicalAttachment.Create(Guid.NewGuid(), "lab.pdf", "application/pdf", 1024,
            "notes/x/y.pdf", new string('a', 64), ClinicalAttachmentScanStatus.Clean, now, Guid.NewGuid(), now);

        att.SoftDelete(Guid.NewGuid(), now.AddMinutes(1));

        Assert.True(att.IsDeleted);
        Assert.NotNull(att.DeletedAt);
        Assert.NotNull(att.DeletedByUserId);
    }

    [Fact]
    public void SoftDelete_Should_Be_Idempotent_When_Already_Deleted()
    {
        var now = DateTimeOffset.UtcNow;
        var att = ClinicalAttachment.Create(Guid.NewGuid(), "lab.pdf", "application/pdf", 1024,
            "notes/x/y.pdf", new string('a', 64), ClinicalAttachmentScanStatus.Clean, now, Guid.NewGuid(), now);
        att.SoftDelete(Guid.NewGuid(), now);
        var firstDeletedAt = att.DeletedAt;

        att.SoftDelete(Guid.NewGuid(), now.AddHours(1)); // segunda vez: no-op

        Assert.Equal(firstDeletedAt, att.DeletedAt); // no se re-escribe
    }

    [Fact]
    public void SoftDelete_Should_Throw_On_Empty_User()
    {
        var now = DateTimeOffset.UtcNow;
        var att = ClinicalAttachment.Create(Guid.NewGuid(), "lab.pdf", "application/pdf", 1024,
            "notes/x/y.pdf", new string('a', 64), ClinicalAttachmentScanStatus.Clean, now, Guid.NewGuid(), now);
        Assert.Throws<DomainException>(() => att.SoftDelete(Guid.Empty, now));
    }
}