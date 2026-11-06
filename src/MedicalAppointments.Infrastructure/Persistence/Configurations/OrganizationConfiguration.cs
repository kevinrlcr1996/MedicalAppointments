using MedicalAppointments.Domain.MedicalStructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalAppointments.Infrastructure.Persistence.Configurations;

public sealed class OrganizationConfiguration : AggregateRootConfiguration<Organization>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("Organizations");

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.NormalizedName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.LegalName).HasMaxLength(200);
        builder.Property(x => x.TaxId).HasMaxLength(100);
        builder.Property(x => x.NormalizedTaxId).HasMaxLength(100);
        builder.Property(x => x.Email).HasMaxLength(320);
        builder.Property(x => x.Phone).HasMaxLength(50);
        builder.Property(x => x.Address).HasMaxLength(500);
        builder.Property(x => x.City).HasMaxLength(200);
        builder.Property(x => x.Country).HasMaxLength(100);
        builder.Property(x => x.IsActive).IsRequired();

        builder.HasIndex(x => x.NormalizedName)
            .IsUnique()
            .HasDatabaseName("IX_Organizations_NormalizedName");

        builder.HasIndex(x => x.NormalizedTaxId)
            .IsUnique()
            .HasFilter("[NormalizedTaxId] IS NOT NULL")
            .HasDatabaseName("IX_Organizations_NormalizedTaxId");
    }
}