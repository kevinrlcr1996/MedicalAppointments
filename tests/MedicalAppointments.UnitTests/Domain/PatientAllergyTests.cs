using MedicalAppointments.Domain.ClinicalRecords;
using MedicalAppointments.Domain.Common;

namespace MedicalAppointments.UnitTests.Domain;

public class PatientAllergyTests
{
    private static PatientAllergy NewDraft() => PatientAllergy.Create(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Ana Medica",
        "Penicilina", AllergenClass.Drug, AllergySeverity.Severe, "Anafilaxia", null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

    [Fact] public void Create_Starts_As_Draft_Unresolved() { var a = NewDraft(); Assert.False(a.IsFinal); Assert.False(a.IsResolved); }
    [Fact] public void UpdateDraft_Throws_When_Finalized() { var a = NewDraft(); a.Finalize(Guid.NewGuid(), DateTimeOffset.UtcNow); Assert.Throws<DomainException>(() => a.UpdateDraft("X", AllergenClass.Food, AllergySeverity.Mild, null, null, DateTimeOffset.UtcNow)); }
    [Fact] public void MarkResolved_Requires_Finalized() { var a = NewDraft(); Assert.Throws<DomainException>(() => a.MarkResolved(Guid.NewGuid(), DateTimeOffset.UtcNow)); }
    [Fact] public void MarkResolved_Then_Unresolve_RoundTrips() { var a = NewDraft(); a.Finalize(Guid.NewGuid(), DateTimeOffset.UtcNow); a.MarkResolved(Guid.NewGuid(), DateTimeOffset.UtcNow); Assert.True(a.IsResolved); a.MarkUnresolved(Guid.NewGuid(), DateTimeOffset.UtcNow); Assert.False(a.IsResolved); Assert.Null(a.ResolvedAt); }
    [Fact] public void MarkResolved_Is_Idempotent() { var a = NewDraft(); a.Finalize(Guid.NewGuid(), DateTimeOffset.UtcNow); a.MarkResolved(Guid.NewGuid(), DateTimeOffset.UtcNow); var t = a.ResolvedAt; a.MarkResolved(Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(1)); Assert.Equal(t, a.ResolvedAt); }
    [Fact] public void CreateCorrection_Links_To_Finalized_Original() { var o = NewDraft(); o.Finalize(Guid.NewGuid(), DateTimeOffset.UtcNow); var c = PatientAllergy.CreateCorrection(o, o.PatientId, Guid.NewGuid(), Guid.NewGuid(), "Ana Medica", "Penicilina", AllergenClass.Drug, AllergySeverity.Moderate, null, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow); Assert.Equal(o.Id, c.CorrectsAllergyId); Assert.False(c.IsFinal); Assert.True(o.IsFinal); }
    [Fact] public void CreateCorrection_Throws_When_Original_Not_Finalized() { var o = NewDraft(); Assert.Throws<DomainException>(() => PatientAllergy.CreateCorrection(o, o.PatientId, Guid.NewGuid(), Guid.NewGuid(), "Ana Medica", "X", AllergenClass.Food, AllergySeverity.Mild, null, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)); }
    [Fact] public void Create_Throws_On_Empty_Allergen() => Assert.Throws<DomainException>(() => PatientAllergy.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Ana Medica", "  ", AllergenClass.Drug, AllergySeverity.Mild, null, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
}