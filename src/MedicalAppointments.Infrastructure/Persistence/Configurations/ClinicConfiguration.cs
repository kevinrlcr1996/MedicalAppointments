using MedicalAppointments.Domain.MedicalStructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalAppointments.Infrastructure.Persistence.Configurations;

public sealed class ClinicConfiguration : AggregateRootConfiguration<Clinic>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Clinic> builder)
    {
        builder.ToTable("Clinics");

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.NormalizedName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(100);
        builder.Property(x => x.NormalizedCode).HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.Address).HasMaxLength(500);
        builder.Property(x => x.City).HasMaxLength(200);
        builder.Property(x => x.Country).HasMaxLength(100);
        builder.Property(x => x.Phone).HasMaxLength(50);
        builder.Property(x => x.Email).HasMaxLength(320);
        builder.Property(x => x.TimeZoneId).HasMaxLength(100);
        builder.Property(x => x.IsActive).IsRequired();

        builder.HasOne(x => x.Organization)
            .WithMany()
            .HasForeignKey(x => x.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.OrganizationId, x.NormalizedName })
            .IsUnique()
            .HasDatabaseName("IX_Clinics_OrganizationId_NormalizedName");

        builder.HasIndex(x => new { x.OrganizationId, x.NormalizedCode })
            .IsUnique()
            .HasFilter("[NormalizedCode] IS NOT NULL")
            .HasDatabaseName("IX_Clinics_OrganizationId_NormalizedCode");

        builder.HasIndex(x => x.OrganizationId)
            .HasDatabaseName("IX_Clinics_OrganizationId");
    }
}