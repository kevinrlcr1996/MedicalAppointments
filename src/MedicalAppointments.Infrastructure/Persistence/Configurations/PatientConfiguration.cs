using MedicalAppointments.Domain.MedicalStructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalAppointments.Infrastructure.Persistence.Configurations;

public sealed class PatientConfiguration : AggregateRootConfiguration<Patient>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients");

        builder.Property(x => x.FirstName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.NormalizedFullName).HasMaxLength(401).IsRequired();
        builder.Property(x => x.Gender)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(x => x.BloodType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(x => x.IdentificationNumber).HasMaxLength(100);
        builder.Property(x => x.NormalizedIdentificationNumber).HasMaxLength(100);
        builder.Property(x => x.Phone).HasMaxLength(50);
        builder.Property(x => x.Email).HasMaxLength(320);
        builder.Property(x => x.Address).HasMaxLength(500);
        builder.Property(x => x.EmergencyContact).HasMaxLength(200);
        builder.Property(x => x.EmergencyPhone).HasMaxLength(50);
        builder.Property(x => x.InsuranceProvider).HasMaxLength(200);
        builder.Property(x => x.InsuranceNumber).HasMaxLength(100);

        builder.HasIndex(x => x.NormalizedIdentificationNumber)
            .IsUnique()
            .HasFilter("[NormalizedIdentificationNumber] IS NOT NULL")
            .HasDatabaseName("IX_Patients_NormalizedIdentificationNumber");

        builder.HasIndex(x => x.UserId)
            .IsUnique()
            .HasFilter("[UserId] IS NOT NULL")
            .HasDatabaseName("IX_Patients_UserId");

        builder.HasIndex(x => x.NormalizedFullName)
            .HasDatabaseName("IX_Patients_NormalizedFullName");
    }
}