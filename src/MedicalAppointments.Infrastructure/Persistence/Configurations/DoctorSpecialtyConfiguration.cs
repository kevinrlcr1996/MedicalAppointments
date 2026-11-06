using MedicalAppointments.Domain.MedicalStructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalAppointments.Infrastructure.Persistence.Configurations;

public sealed class DoctorSpecialtyConfiguration : IEntityTypeConfiguration<DoctorSpecialty>
{
    public void Configure(EntityTypeBuilder<DoctorSpecialty> builder)
    {
        builder.ToTable("DoctorSpecialties");

        builder.HasKey(x => new { x.DoctorId, x.SpecialtyId });

        builder.Property(x => x.AssignedAt).IsRequired();

        builder.HasOne(x => x.Doctor)
            .WithMany(x => x.Specialties)
            .HasForeignKey(x => x.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Specialty)
            .WithMany()
            .HasForeignKey(x => x.SpecialtyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.SpecialtyId)
            .HasDatabaseName("IX_DoctorSpecialties_SpecialtyId");
    }
}