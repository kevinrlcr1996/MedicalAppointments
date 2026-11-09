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
}