using MedicalAppointments.Domain.MedicalStructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalAppointments.Infrastructure.Persistence.Configurations;

public sealed class DoctorClinicConfiguration : IEntityTypeConfiguration<DoctorClinic>
{
    public void Configure(EntityTypeBuilder<DoctorClinic> builder)
    {
        builder.ToTable("DoctorClinics");

        builder.HasKey(x => new { x.DoctorId, x.ClinicId });

        builder.Property(x => x.IsPrimary).IsRequired();
        builder.Property(x => x.AssignedAt).IsRequired();

        builder.HasOne(x => x.Doctor)
            .WithMany(x => x.Clinics)
            .HasForeignKey(x => x.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Clinic)
            .WithMany()
            .HasForeignKey(x => x.ClinicId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.ClinicId)
            .HasDatabaseName("IX_DoctorClinics_ClinicId");
    }
}