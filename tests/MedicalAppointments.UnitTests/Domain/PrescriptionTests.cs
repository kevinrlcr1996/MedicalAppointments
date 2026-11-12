using MedicalAppointments.Domain.ClinicalRecords;
using MedicalAppointments.Domain.Common;
using Xunit;

namespace MedicalAppointments.UnitTests.Domain;

public class PrescriptionTests
{
    private static Prescription NewDraft() => Prescription.Create(
        patientId: Guid.NewGuid(),
        authorUserId: Guid.NewGuid(),
        authorDoctorId: Guid.NewGuid(),
        authorDisplayName: "Dra. Ana Medica",
        medicationName: "Amoxicilina",
        strength: "500 mg",
        dosageForm: "Cápsula",
        frequency: "1-0-1",
        route: "Oral",
        quantity: 21,
        refillsAllowed: 0,
        daysSupply: 7,
        instructions: "Tomar con alimentos",
        note: null,
        recordedAtUtc: DateTimeOffset.UtcNow,
        now: DateTimeOffset.UtcNow);

    // ---- create / draft ----
    [Fact]
    public void Create_starts_Draft_and_not_final()
    {
        var p = NewDraft();
        Assert.Equal(PrescriptionStatus.Draft, p.Status);
        Assert.False(p.IsFinal);
        Assert.Null(p.IssuedAt);
        Assert.Null(p.CorrectsPrescriptionId);
        Assert.Null(p.RenewedFromPrescriptionId);
    }

    // ---- UpdateDraft: ok en draft, 409/throw cuando issued (inmutabilidad de contenido) ----
    [Fact]
    public void UpdateDraft_ok_on_draft()
    {
        var p = NewDraft();
        p.UpdateDraft("Ibuprofeno", "400 mg", null, "a demanda", null, null, null, null, null, null, DateTimeOffset.UtcNow);
        Assert.Equal("Ibuprofeno", p.MedicationName);
        Assert.Equal("400 mg", p.Strength);
    }

    [Fact]
    public void UpdateDraft_throws_when_issued()
    {
        var p = NewDraft();
        p.Issue(Guid.NewGuid(), DateTimeOffset.UtcNow);
        Assert.Throws<DomainException>(() =>
            p.UpdateDraft("X", null, null, null, null, null, null, null, null, null, DateTimeOffset.UtcNow));
    }

    // ---- Issue: sella contenido + timestamps; throw si ya emitida ----
    [Fact]
    public void Issue_sets_final_and_timestamps()
    {
        var p = NewDraft();
        var u = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        p.Issue(u, now);
        Assert.Equal(PrescriptionStatus.Issued, p.Status);
        Assert.True(p.IsFinal);
        Assert.Equal(u, p.IssuedByUserId);
        Assert.Equal(now, p.IssuedAt);
    }

    [Fact]
    public void Issue_throws_when_already_issued()
    {
        var p = NewDraft();
        p.Issue(Guid.NewGuid(), DateTimeOffset.UtcNow);
        Assert.Throws<DomainException>(() => p.Issue(Guid.NewGuid(), DateTimeOffset.UtcNow));
    }

    // ---- Dispense: requiere Issued; idempotente ----
    [Fact]
    public void Dispense_throws_when_draft()
    {
        var p = NewDraft();
        Assert.Throws<DomainException>(() => p.Dispense(Guid.NewGuid(), DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Dispense_from_issued_ok_and_idempotent()
    {
        var p = NewDraft();
        p.Issue(Guid.NewGuid(), DateTimeOffset.UtcNow);
        p.Dispense(Guid.NewGuid(), DateTimeOffset.UtcNow);
        Assert.Equal(PrescriptionStatus.Dispensed, p.Status);
        var at = p.DispensedAt;
        p.Dispense(Guid.NewGuid(), DateTimeOffset.UtcNow); // no-op
        Assert.Equal(at, p.DispensedAt);
    }

    // ---- Expire: requiere Issued o Dispensed; idempotente ----
    [Fact]
    public void Expire_throws_when_draft()
    {
        var p = NewDraft();
        Assert.Throws<DomainException>(() => p.Expire(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Expire_from_issued_ok_and_idempotent()
    {
        var p = NewDraft();
        p.Issue(Guid.NewGuid(), DateTimeOffset.UtcNow);
        p.Expire(DateTimeOffset.UtcNow);
        Assert.Equal(PrescriptionStatus.Expired, p.Status);
        var first = p.ExpiredAt;
        p.Expire(DateTimeOffset.UtcNow); // no-op, timestamp estable
        Assert.Equal(first, p.ExpiredAt);
    }

    // ---- Cancel: permite Draft/Issued/Dispensed; idempotente; bloquea Expired ----
    [Fact]
    public void Cancel_from_draft_ok_and_idempotent()
    {
        var p = NewDraft();
        p.Cancel(Guid.NewGuid(), DateTimeOffset.UtcNow);
        Assert.Equal(PrescriptionStatus.Cancelled, p.Status);
        var at = p.CancelledAt;
        p.Cancel(Guid.NewGuid(), DateTimeOffset.UtcNow); // no-op
        Assert.Equal(at, p.CancelledAt);
    }

    [Fact]
    public void Cancel_throws_when_expired()
    {
        var p = NewDraft();
        p.Issue(Guid.NewGuid(), DateTimeOffset.UtcNow);
        p.Expire(DateTimeOffset.UtcNow);
        Assert.Throws<DomainException>(() => p.Cancel(Guid.NewGuid(), DateTimeOffset.UtcNow));
    }

    // ---- Renew: requiere Issued o Dispensed; nueva ligada YA emitida; original intacto ----
    [Fact]
    public void Renew_throws_when_draft()
    {
        var p = NewDraft();
        Assert.Throws<DomainException>(() =>
            Prescription.Renew(p, Guid.NewGuid(), Guid.NewGuid(), "Dra. Ana Medica", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Renew_from_issued_produces_linked_issued_copy_original_intact()
    {
        var p = NewDraft();
        p.Issue(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var r = Prescription.Renew(p, Guid.NewGuid(), Guid.NewGuid(), "Dra. Ana Medica", DateTimeOffset.UtcNow);
        Assert.Equal(PrescriptionStatus.Issued, r.Status);
        Assert.True(r.IsFinal);
        Assert.Equal(p.Id, r.RenewedFromPrescriptionId);
        Assert.Equal(p.MedicationName, r.MedicationName);   // contenido copiado
        Assert.Equal(p.Strength, r.Strength);
        Assert.NotEqual(p.Id, r.Id);                         // fila nueva
        Assert.Equal(PrescriptionStatus.Issued, p.Status);   // original intacto
    }

    // ---- CreateCorrection: requiere original final; nueva ligada Draft editable; original intacto ----
    [Fact]
    public void CreateCorrection_throws_when_original_not_final()
    {
        var p = NewDraft();
        Assert.Throws<DomainException>(() => Prescription.CreateCorrection(
            p, p.PatientId, Guid.NewGuid(), Guid.NewGuid(), "Dra. Ana Medica",
            "Amoxicilina", "875 mg", null, "1-0-1", null, 14, 0, 7, null, null,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void CreateCorrection_from_issued_produces_linked_draft_editable_original_intact()
    {
        var p = NewDraft();
        p.Issue(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var c = Prescription.CreateCorrection(
            p, p.PatientId, Guid.NewGuid(), Guid.NewGuid(), "Dra. Ana Medica",
            "Amoxicilina", "875 mg", null, "1-0-1", null, 14, 0, 7, null, null,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        Assert.Equal(PrescriptionStatus.Draft, c.Status);
        Assert.False(c.IsFinal);
        Assert.Equal(p.Id, c.CorrectsPrescriptionId);
        Assert.NotEqual(p.Id, c.Id);
        // editable por ser borrador:
        c.UpdateDraft("Amoxicilina", "875 mg", null, "1-0-1", null, 14, 0, 7, "corregida", null, DateTimeOffset.UtcNow);
        Assert.Equal("corregida", c.Instructions);
        // original intacto:
        Assert.Equal(PrescriptionStatus.Issued, p.Status);
        Assert.False(p.IsFinal == false && p.Status == PrescriptionStatus.Issued); // sanity: emitida sigue final
        Assert.True(p.IsFinal);
    }

    // ---- guards de contenido ----
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void MedicationName_required(string name)
    {
        Assert.Throws<DomainException>(() => Prescription.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "x", name,
            null, null, null, null, null, null, null, null, null,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Negative_quantity_throws()
    {
        Assert.Throws<DomainException>(() => Prescription.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "x", "A",
            null, null, null, null, -1, null, null, null, null,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Negative_refills_or_days_throws()
    {
        Assert.Throws<DomainException>(() => Prescription.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "x", "A",
            null, null, null, null, null, -1, null, null, null,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        Assert.Throws<DomainException>(() => Prescription.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "x", "A",
            null, null, null, null, null, null, -1, null, null,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Empty_author_user_or_doctor_or_displayname_throws()
    {
        Assert.Throws<DomainException>(() => Prescription.Create(
            Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), "x", "A",
            null, null, null, null, null, null, null, null, null,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        Assert.Throws<DomainException>(() => Prescription.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, "x", "A",
            null, null, null, null, null, null, null, null, null,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        Assert.Throws<DomainException>(() => Prescription.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "  ", "A",
            null, null, null, null, null, null, null, null, null,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Empty_patient_throws()
    {
        Assert.Throws<DomainException>(() => Prescription.Create(
            Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), "x", "A",
            null, null, null, null, null, null, null, null, null,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Strength_over_limit_throws()
    {
        Assert.Throws<DomainException>(() => Prescription.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "x", "A",
            new string('s', 101), null, null, null, null, null, null, null, null,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
    }
}