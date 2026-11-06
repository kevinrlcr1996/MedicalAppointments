using MedicalAppointments.Domain.MedicalStructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalAppointments.Infrastructure.Persistence.Configurations;

public sealed class SpecialtyConfiguration : AggregateRootConfiguration<Specialty>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Specialty> builder)
    {
        builder.ToTable("Specialties");

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.NormalizedName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(100);
        builder.Property(x => x.NormalizedCode).HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.IsActive).IsRequired();

        builder.HasIndex(x => x.NormalizedName)
            .IsUnique()
            .HasDatabaseName("IX_Specialties_NormalizedName");

        builder.HasIndex(x => x.NormalizedCode)
            .IsUnique()
            .HasFilter("[NormalizedCode] IS NOT NULL")
            .HasDatabaseName("IX_Specialties_NormalizedCode");
    }
}