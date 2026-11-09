using MedicalAppointments.Domain.ClinicalRecords;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalAppointments.Infrastructure.Persistence.Configurations;

public sealed class ClinicalAttachmentConfiguration : AggregateRootConfiguration<ClinicalAttachment>
{
    protected override void ConfigureEntity(EntityTypeBuilder<ClinicalAttachment> builder)
    {
        builder.ToTable("ClinicalAttachments");

        builder.Property(x => x.FileName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(200).IsRequired();
        builder.Property(x => x.SizeBytes).IsRequired();
        builder.Property(x => x.StorageKey).HasMaxLength(400).IsRequired();
        builder.Property(x => x.Sha256Hex).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ScanStatus).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
        builder.Property(x => x.DeletedAt).IsRequired(false);
        builder.Property(x => x.DeletedByUserId).IsRequired(false);

        // Direccionalidad: el adjunto conoce la nota; la nota NO navega a adjuntos (no se toca ClinicalNote de Fase 8).
        builder.HasOne(x => x.Note)
            .WithMany()
            .HasForeignKey(x => x.NoteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.NoteId).HasDatabaseName("IX_ClinicalAttachments_NoteId");
        builder.HasIndex(x => x.ScanStatus).HasDatabaseName("IX_ClinicalAttachments_ScanStatus");
        builder.HasIndex(x => x.Sha256Hex).HasDatabaseName("IX_ClinicalAttachments_Sha256");
        // Índice parcial para que las listas/conteos filtren borrados rápido:
        builder.HasIndex(x => new { x.NoteId, x.IsDeleted }).HasDatabaseName("IX_ClinicalAttachments_NoteId_IsDeleted");
    }
}