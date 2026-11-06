using MedicalAppointments.Domain.MedicalStructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalAppointments.Infrastructure.Persistence.Configurations;

public sealed class ResourceConfiguration : AggregateRootConfiguration<Resource>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Resource> builder)
    {
        builder.ToTable("Resources");

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.NormalizedName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(100);
        builder.Property(x => x.NormalizedCode).HasMaxLength(100);
        builder.Property(x => x.Kind)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.IsActive).IsRequired();

        builder.HasOne(x => x.Clinic)
            .WithMany()
            .HasForeignKey(x => x.ClinicId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ClinicId, x.NormalizedName })
            .IsUnique()
            .HasDatabaseName("IX_Resources_ClinicId_NormalizedName");

        builder.HasIndex(x => new { x.ClinicId, x.NormalizedCode })
            .IsUnique()
            .HasFilter("[NormalizedCode] IS NOT NULL")
            .HasDatabaseName("IX_Resources_ClinicId_NormalizedCode");

        builder.HasIndex(x => x.ClinicId)
            .HasDatabaseName("IX_Resources_ClinicId");
    }
}