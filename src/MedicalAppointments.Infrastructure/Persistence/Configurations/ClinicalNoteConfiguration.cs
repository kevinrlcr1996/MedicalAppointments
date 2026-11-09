using MedicalAppointments.Domain.ClinicalRecords;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalAppointments.Infrastructure.Persistence.Configurations;

public sealed class ClinicalNoteConfiguration : AggregateRootConfiguration<ClinicalNote>
{
    protected override void ConfigureEntity(EntityTypeBuilder<ClinicalNote> builder)
    {
        builder.ToTable("ClinicalNotes");

        builder.Property(x => x.Category).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Content).IsRequired();
        builder.Property(x => x.AuthorDisplayName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.IsFinal).IsRequired();
        builder.Property(x => x.OccurredAtUtc).IsRequired();

        // Direccionalidad: la nota conoce la cita; la cita NO conoce las notas (no se toca Appointment).
        builder.HasOne(x => x.Appointment)
            .WithMany()
            .HasForeignKey(x => x.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AuthorDoctor)
            .WithMany()
            .HasForeignKey(x => x.AuthorDoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CorrectsNote)
            .WithMany()
            .HasForeignKey(x => x.CorrectsNoteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.AppointmentId).HasDatabaseName("IX_ClinicalNotes_AppointmentId");
        builder.HasIndex(x => new { x.AppointmentId, x.CreatedAt }).HasDatabaseName("IX_ClinicalNotes_Appointment_Evolution");
        builder.HasIndex(x => x.AuthorDoctorId).HasDatabaseName("IX_ClinicalNotes_AuthorDoctorId");
        builder.HasIndex(x => x.CorrectsNoteId).HasDatabaseName("IX_ClinicalNotes_CorrectsNoteId");
    }
}