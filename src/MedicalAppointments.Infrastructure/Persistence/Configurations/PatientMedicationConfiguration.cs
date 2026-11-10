using MedicalAppointments.Domain.ClinicalRecords;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalAppointments.Infrastructure.Persistence.Configurations;

public sealed class PatientMedicationConfiguration : AggregateRootConfiguration<PatientMedication>
{
    protected override void ConfigureEntity(EntityTypeBuilder<PatientMedication> builder)
    {
        builder.ToTable("PatientMedications");
        builder.Property(x => x.AuthorDisplayName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.MedicationName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Strength).HasMaxLength(100);
        builder.Property(x => x.DosageForm).HasMaxLength(100);
        builder.Property(x => x.Frequency).HasMaxLength(100);
        builder.Property(x => x.Route).HasMaxLength(100);
        builder.Property(x => x.Indication).HasMaxLength(1000);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasOne(x => x.Patient).WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.AuthorDoctor).WithMany().HasForeignKey(x => x.AuthorDoctorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.CorrectsMedication).WithMany().HasForeignKey(x => x.CorrectsMedicationId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.PatientId).HasDatabaseName("IX_PatientMedications_PatientId");
        builder.HasIndex(x => new { x.PatientId, x.Status }).HasDatabaseName("IX_PatientMedications_PatientId_Status");
        builder.HasIndex(x => x.AuthorDoctorId).HasDatabaseName("IX_PatientMedications_AuthorDoctorId");
        builder.HasIndex(x => x.CorrectsMedicationId).HasDatabaseName("IX_PatientMedications_CorrectsMedicationId");
    }
}