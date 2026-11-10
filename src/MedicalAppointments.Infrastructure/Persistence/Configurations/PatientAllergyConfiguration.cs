using MedicalAppointments.Domain.ClinicalRecords;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalAppointments.Infrastructure.Persistence.Configurations;

public sealed class PatientAllergyConfiguration : AggregateRootConfiguration<PatientAllergy>
{
    protected override void ConfigureEntity(EntityTypeBuilder<PatientAllergy> builder)
    {
        builder.ToTable("PatientAllergies");

        builder.Property(x => x.AuthorDisplayName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Allergen).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Reaction).HasMaxLength(1000);
        builder.Property(x => x.AllergenClass).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Severity).HasConversion<string>().HasMaxLength(20).IsRequired();

        // Rule 6: el registro conoce al paciente; el paciente NO navega a alergias.
        builder.HasOne(x => x.Patient)
            .WithMany()
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AuthorDoctor)
            .WithMany()
            .HasForeignKey(x => x.AuthorDoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CorrectsAllergy)
            .WithMany()
            .HasForeignKey(x => x.CorrectsAllergyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.PatientId).HasDatabaseName("IX_PatientAllergies_PatientId");
        builder.HasIndex(x => new { x.PatientId, x.IsResolved }).HasDatabaseName("IX_PatientAllergies_PatientId_IsResolved");
        builder.HasIndex(x => x.AuthorDoctorId).HasDatabaseName("IX_PatientAllergies_AuthorDoctorId");
        builder.HasIndex(x => x.CorrectsAllergyId).HasDatabaseName("IX_PatientAllergies_CorrectsAllergyId");
    }
}