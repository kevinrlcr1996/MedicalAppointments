using MedicalAppointments.Domain.ClinicalRecords;
using MedicalAppointments.Domain.Common;
using Xunit;

namespace MedicalAppointments.UnitTests.Domain;

public class DiagnosisTests
{
    private static Diagnosis NewDraft(string code = "I10", string term = "Hipertensión esencial") => Diagnosis.Create(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Ana Medica",
        code, term, DiagnosisCategory.Primary, null, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

    [Fact] public void Create_Starts_As_Draft() => Assert.False(NewDraft().IsFinal);

    [Fact]
    public void Create_Normalizes_Code_To_Uppercase()
        => Assert.Equal("E11.9", NewDraft("e11.9").Code);

    [Fact]
    public void UpdateDraft_Throws_When_Finalized()
    {
        var d = NewDraft();
        d.Finalize(Guid.NewGuid(), DateTimeOffset.UtcNow);
        Assert.Throws<DomainException>(() => d.UpdateDraft("I11", "Otra", DiagnosisCategory.Secondary, null, null, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void CreateCorrection_Links_To_Finalized_Original()
    {
        var orig = NewDraft();
        orig.Finalize(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var corr = Diagnosis.CreateCorrection(orig, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Ana Medica", "I11", "Hipertensión con cardiopatía", DiagnosisCategory.Primary, null, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        Assert.Equal(orig.Id, corr.CorrectsDiagnosisId);
        Assert.False(corr.IsFinal);
        Assert.True(orig.IsFinal);
    }

    [Fact]
    public void CreateCorrection_Throws_When_Original_Not_Finalized()
    {
        var orig = NewDraft();
        Assert.Throws<DomainException>(() => Diagnosis.CreateCorrection(orig, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Ana Medica", "I11", "X", DiagnosisCategory.Primary, null, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
    }

    [Theory]
    [InlineData("123")]      // sin letra inicial
    [InlineData("U07.1")]    // U reservado
    [InlineData("A00.1.2.3.4")] // demasiado después del punto
    [InlineData("")]         // vacío
    [InlineData("I1")]       // faltan dígitos
    public void Create_Throws_On_Invalid_Icd10(string code)
        => Assert.Throws<DomainException>(() => NewDraft(code));

    [Fact]
    public void Create_Throws_On_Empty_DisplayTerm()
        => Assert.Throws<DomainException>(() => NewDraft("I10", "  "));
}