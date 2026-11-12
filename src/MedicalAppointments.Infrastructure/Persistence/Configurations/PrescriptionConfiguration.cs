using MedicalAppointments.Domain.ClinicalRecords;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalAppointments.Infrastructure.Persistence.Configurations;

public sealed class PrescriptionConfiguration : AggregateRootConfiguration<Prescription>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Prescription> builder)
    {
        builder.ToTable("Prescriptions");
        builder.Property(x => x.AuthorDisplayName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.MedicationName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Strength).HasMaxLength(100);
        builder.Property(x => x.DosageForm).HasMaxLength(100);
        builder.Property(x => x.Frequency).HasMaxLength(100);
        builder.Property(x => x.Route).HasMaxLength(100);
        builder.Property(x => x.Instructions).HasMaxLength(2000);
        builder.Property(x => x.Note).HasMaxLength(2000);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasOne(x => x.Patient).WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.AuthorDoctor).WithMany().HasForeignKey(x => x.AuthorDoctorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.CorrectsPrescription).WithMany().HasForeignKey(x => x.CorrectsPrescriptionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.RenewedFromPrescription).WithMany().HasForeignKey(x => x.RenewedFromPrescriptionId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.PatientId).HasDatabaseName("IX_Prescriptions_PatientId");
        builder.HasIndex(x => new { x.PatientId, x.Status }).HasDatabaseName("IX_Prescriptions_PatientId_Status");
        builder.HasIndex(x => x.AuthorDoctorId).HasDatabaseName("IX_Prescriptions_AuthorDoctorId");
        builder.HasIndex(x => x.CorrectsPrescriptionId).HasDatabaseName("IX_Prescriptions_CorrectsPrescriptionId");
        builder.HasIndex(x => x.RenewedFromPrescriptionId).HasDatabaseName("IX_Prescriptions_RenewedFromPrescriptionId");
    }
}