using MedicalAppointments.Domain.ClinicalRecords;
using MedicalAppointments.Domain.Common;
using Xunit;

namespace MedicalAppointments.UnitTests.Domain;

public class VitalSignsTests
{
    private static VitalSignsMeasurements M(int? sys = 120, int? dia = 80, int? hr = 72, decimal? temp = 36.8m, int? spo2 = 98, int? pain = 0)
        => new(sys, dia, hr, temp, VitalSignsTemperatureMethod.Oral, VitalSignsPosition.Sitting, 16, spo2, 70m, 170m, pain, null);

    private static VitalSigns NewDraft() => VitalSigns.Create(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Ana Medica", M(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

    [Fact] public void Create_Starts_As_Draft() => Assert.False(NewDraft().IsFinal);

    [Fact]
    public void UpdateDraft_Throws_When_Finalized()
    {
        var v = NewDraft();
        v.Finalize(Guid.NewGuid(), DateTimeOffset.UtcNow);
        Assert.Throws<DomainException>(() => v.UpdateDraft(M(130, 85), DateTimeOffset.UtcNow));
    }

    [Fact]
    public void CreateCorrection_Links_To_Finalized_Original()
    {
        var orig = NewDraft();
        orig.Finalize(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var corr = VitalSigns.CreateCorrection(orig, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Ana Medica", M(130, 85), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        Assert.Equal(orig.Id, corr.CorrectsVitalSignsId);
        Assert.False(corr.IsFinal);
        Assert.True(orig.IsFinal); // original intacto
    }

    [Fact]
    public void CreateCorrection_Throws_When_Original_Not_Finalized()
    {
        var orig = NewDraft();
        Assert.Throws<DomainException>(() => VitalSigns.CreateCorrection(orig, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Ana Medica", M(), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Validate_Throws_On_SpO2_Out_Of_Range()
        => Assert.Throws<DomainException>(() => VitalSigns.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Ana Medica", M(spo2: 120), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

    [Fact]
    public void Validate_Throws_When_Systolic_Not_Greater_Than_Diastolic()
        => Assert.Throws<DomainException>(() => VitalSigns.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Ana Medica", M(sys: 80, dia: 90), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
}