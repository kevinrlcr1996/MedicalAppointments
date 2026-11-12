using MedicalAppointments.Domain.ClinicalRecords;
using MedicalAppointments.Domain.Common;

namespace MedicalAppointments.UnitTests.Domain;

public class ClinicalDocumentTests
{
    private static ClinicalDocument NewDoc() => ClinicalDocument.Create(
        prescriptionId: Guid.NewGuid(),
        fileName: "receta.pdf",
        contentType: "application/pdf",
        sizeBytes: 1024,
        storageKey: "documents/{guid}.pdf",
        sha256Hex: new string('a', 64),
        generatedByUserId: Guid.NewGuid(),
        now: DateTimeOffset.UtcNow);

    [Fact]
    public void Create_persists_metadata_and_is_immutable_no_update_setters()
    {
        var d = NewDoc();
        Assert.Equal("receta.pdf", d.FileName);
        Assert.Equal(1024, d.SizeBytes);
        Assert.Equal(new string('a', 64), d.Sha256Hex);
        // inmutable (D2): no hay UpdateDraft/SoftDelete en el aggregate — solo Create.
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_fileName_throws(string fn) =>
        Assert.Throws<DomainException>(() => ClinicalDocument.Create(
            Guid.NewGuid(), fn, "application/pdf", 1, "k", new string('a', 64), Guid.NewGuid(), DateTimeOffset.UtcNow));

    [Fact]
    public void Zero_or_negative_size_throws() =>
        Assert.Throws<DomainException>(() => ClinicalDocument.Create(
            Guid.NewGuid(), "r.pdf", "application/pdf", 0, "k", new string('a', 64), Guid.NewGuid(), DateTimeOffset.UtcNow));

    [Fact]
    public void Bad_sha256_length_or_nonhex_throws()
    {
        Assert.Throws<DomainException>(() => ClinicalDocument.Create(
            Guid.NewGuid(), "r.pdf", "application/pdf", 1, "k", new string('a', 63), Guid.NewGuid(), DateTimeOffset.UtcNow));
        Assert.Throws<DomainException>(() => ClinicalDocument.Create(
            Guid.NewGuid(), "r.pdf", "application/pdf", 1, "k", new string('g', 64), Guid.NewGuid(), DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Empty_storageKey_or_prescription_or_user_throws()
    {
        Assert.Throws<DomainException>(() => ClinicalDocument.Create(
            Guid.NewGuid(), "r.pdf", "application/pdf", 1, "", new string('a', 64), Guid.NewGuid(), DateTimeOffset.UtcNow));
        Assert.Throws<DomainException>(() => ClinicalDocument.Create(
            Guid.Empty, "r.pdf", "application/pdf", 1, "k", new string('a', 64), Guid.NewGuid(), DateTimeOffset.UtcNow));
        Assert.Throws<DomainException>(() => ClinicalDocument.Create(
            Guid.NewGuid(), "r.pdf", "application/pdf", 1, "k", new string('a', 64), Guid.Empty, DateTimeOffset.UtcNow));
    }
}