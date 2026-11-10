using MedicalAppointments.Domain.ClinicalRecords;
using MedicalAppointments.Domain.Common;

namespace MedicalAppointments.UnitTests.Domain;

public class PatientMedicationTests
{
    private static PatientMedication NewDraft() => PatientMedication.Create(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Ana Medica",
        "Metformina", "500 mg", "Tableta", "cada 12 horas", "oral", null, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

    [Fact] public void Create_Starts_Active_Draft() { var m = NewDraft(); Assert.Equal(MedicationStatus.Active, m.Status); Assert.False(m.IsFinal); }
    [Fact] public void UpdateDraft_Throws_When_Finalized() { var m = NewDraft(); m.Finalize(Guid.NewGuid(), DateTimeOffset.UtcNow); Assert.Throws<DomainException>(() => m.UpdateDraft("X", null, null, null, null, null, null, DateTimeOffset.UtcNow)); }
    [Fact] public void Suspend_Requires_Finalized() { var m = NewDraft(); Assert.Throws<DomainException>(() => m.Suspend(Guid.NewGuid(), DateTimeOffset.UtcNow)); }
    [Fact] public void Suspend_Then_Resume_RoundTrips() { var m = NewDraft(); m.Finalize(Guid.NewGuid(), DateTimeOffset.UtcNow); m.Suspend(Guid.NewGuid(), DateTimeOffset.UtcNow); Assert.Equal(MedicationStatus.Suspended, m.Status); m.Resume(Guid.NewGuid(), DateTimeOffset.UtcNow); Assert.Equal(MedicationStatus.Active, m.Status); Assert.Null(m.SuspendedAt); }
    [Fact] public void Discontinue_From_Suspended_Ok_And_Idempotent() { var m = NewDraft(); m.Finalize(Guid.NewGuid(), DateTimeOffset.UtcNow); m.Suspend(Guid.NewGuid(), DateTimeOffset.UtcNow); m.Discontinue(Guid.NewGuid(), DateTimeOffset.UtcNow); Assert.Equal(MedicationStatus.Discontinued, m.Status); var t = m.DiscontinuedAt; m.Discontinue(Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(1)); Assert.Equal(t, m.DiscontinuedAt); }
    [Fact] public void Suspend_Throws_When_Discontinued() { var m = NewDraft(); m.Finalize(Guid.NewGuid(), DateTimeOffset.UtcNow); m.Discontinue(Guid.NewGuid(), DateTimeOffset.UtcNow); Assert.Throws<DomainException>(() => m.Suspend(Guid.NewGuid(), DateTimeOffset.UtcNow)); }
    [Fact] public void CreateCorrection_Links_To_Finalized_Original() { var o = NewDraft(); o.Finalize(Guid.NewGuid(), DateTimeOffset.UtcNow); var c = PatientMedication.CreateCorrection(o, o.PatientId, Guid.NewGuid(), Guid.NewGuid(), "Ana Medica", "Metformina", "850 mg", null, null, null, null, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow); Assert.Equal(o.Id, c.CorrectsMedicationId); Assert.False(c.IsFinal); Assert.True(o.IsFinal); }
    [Fact] public void Create_Throws_On_Empty_Name() => Assert.Throws<DomainException>(() => PatientMedication.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Ana Medica", "  ", null, null, null, null, null, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
}