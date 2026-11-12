using MedicalAppointments.Domain.ClinicalRecords;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalAppointments.Infrastructure.Persistence.Configurations;

public class PrescriptionDocumentConfiguration : IEntityTypeConfiguration<ClinicalDocument>
{
    public void Configure(EntityTypeBuilder<ClinicalDocument> builder)
    {
        builder.ToTable("ClinicalDocuments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.PrescriptionId).IsRequired();
        builder.Property(x => x.FileName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(200).IsRequired();
        builder.Property(x => x.SizeBytes).IsRequired();
        builder.Property(x => x.StorageKey).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Sha256Hex).HasMaxLength(64).IsRequired();
        builder.Property(x => x.GeneratedByUserId).IsRequired();
        builder.Property(x => x.GeneratedAt).IsRequired();

        // D1: el documento sabe la receta; la receta NO navega a documentos (rule 6). Restrict = no borrar receta con documento.
        builder.HasOne(x => x.Prescription)
            .WithMany()
            .HasForeignKey(x => x.PrescriptionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.PrescriptionId);
        builder.HasIndex(x => x.Sha256Hex);
        // NO indice de ScanStatus (D3: documento generado no se escanea).
    }
}