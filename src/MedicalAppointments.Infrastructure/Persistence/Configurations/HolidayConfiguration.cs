using MedicalAppointments.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalAppointments.Infrastructure.Persistence.Configurations;

public sealed class HolidayConfiguration : AggregateRootConfiguration<Holiday>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Holiday> builder)
    {
        builder.ToTable("Holidays");

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.Date).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();

        builder.HasIndex(x => x.Date)
            .HasDatabaseName("IX_Holidays_Date");

        builder.HasIndex(x => new { x.OrganizationId, x.Date })
            .HasDatabaseName("IX_Holidays_OrganizationId_Date");

        builder.HasIndex(x => new { x.ClinicId, x.Date })
            .HasDatabaseName("IX_Holidays_ClinicId_Date");
    }
}