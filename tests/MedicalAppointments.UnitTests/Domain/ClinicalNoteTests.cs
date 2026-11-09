using MedicalAppointments.Domain.ClinicalRecords;
using MedicalAppointments.Domain.Common;
using Xunit;

namespace MedicalAppointments.UnitTests.Domain;

public class ClinicalNoteTests
{
    [Fact]
    public void Create_Should_Start_As_Draft()
    {
        var now = DateTimeOffset.UtcNow;
        var note = ClinicalNote.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Dr. Test",
            ClinicalNoteCategory.Subjective, "Título", "Contenido", now, now);

        Assert.False(note.IsFinal);
        Assert.Null(note.FinalizedAt);
    }

    [Fact]
    public void UpdateDraft_Should_Throw_When_Finalized()
    {
        var now = DateTimeOffset.UtcNow;
        var note = ClinicalNote.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Dr. Test",
            ClinicalNoteCategory.Subjective, "Título", "Contenido", now, now);

        note.Finalize(Guid.NewGuid(), now);

        Assert.Throws<DomainException>(
            () => note.UpdateDraft(ClinicalNoteCategory.Plan, "Otro", "Otro contenido", now));
    }

    [Fact]
    public void CreateCorrection_Should_Link_To_Finalized_Original()
    {
        var now = DateTimeOffset.UtcNow;
        var original = ClinicalNote.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Dr. Test",
            ClinicalNoteCategory.Assessment, "Diagnóstico", "Contenido", now, now);
        original.Finalize(Guid.NewGuid(), now);

        var correction = ClinicalNote.CreateCorrection(
            original, Guid.NewGuid(), Guid.NewGuid(), "Dr. Test",
            ClinicalNoteCategory.Assessment, "Corrección", "Contenido corregido", now, now);

        Assert.Equal(original.Id, correction.CorrectsNoteId);
        Assert.False(correction.IsFinal);
    }

    [Fact]
    public void CreateCorrection_Should_Throw_When_Original_Not_Finalized()
    {
        var now = DateTimeOffset.UtcNow;
        var draft = ClinicalNote.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Dr. Test",
            ClinicalNoteCategory.Subjective, "Título", "Contenido", now, now);

        Assert.Throws<DomainException>(
            () => ClinicalNote.CreateCorrection(
                draft, Guid.NewGuid(), Guid.NewGuid(), "Dr. Test",
                ClinicalNoteCategory.Subjective, "Corrección", "x", now, now));
    }
}