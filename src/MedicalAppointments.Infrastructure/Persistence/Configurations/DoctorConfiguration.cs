using MedicalAppointments.Domain.MedicalStructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalAppointments.Infrastructure.Persistence.Configurations;

public sealed class DoctorConfiguration : AggregateRootConfiguration<Doctor>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Doctor> builder)
    {
        builder.ToTable("Doctors");

        builder.Property(x => x.FirstName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.NormalizedFullName).HasMaxLength(401).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(320);
        builder.Property(x => x.Phone).HasMaxLength(50);
        builder.Property(x => x.LicenseNumber).HasMaxLength(100).IsRequired();
        builder.Property(x => x.NormalizedLicenseNumber).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LicenseCountry).HasMaxLength(2).IsRequired();
        builder.Property(x => x.NormalizedLicenseCountry).HasMaxLength(2).IsRequired();
        builder.Property(x => x.Biography).HasMaxLength(4000);
        builder.Property(x => x.PhotoUrl).HasMaxLength(2048);
        builder.Property(x => x.Gender)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(x => new { x.NormalizedLicenseNumber, x.NormalizedLicenseCountry })
            .IsUnique()
            .HasDatabaseName("IX_Doctors_NormalizedLicenseNumber_NormalizedLicenseCountry");

        builder.HasIndex(x => x.UserId)
            .IsUnique()
            .HasFilter("[UserId] IS NOT NULL")
            .HasDatabaseName("IX_Doctors_UserId");

        builder.HasIndex(x => x.NormalizedFullName)
            .HasDatabaseName("IX_Doctors_NormalizedFullName");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("IX_Doctors_Status");

        builder.HasMany(x => x.Specialties)
            .WithOne(x => x.Doctor)
            .HasForeignKey(x => x.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Clinics)
            .WithOne(x => x.Doctor)
            .HasForeignKey(x => x.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Languages)
            .WithOne(x => x.Doctor)
            .HasForeignKey(x => x.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}