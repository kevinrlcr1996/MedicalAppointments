using MedicalAppointments.Domain.MedicalStructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalAppointments.Infrastructure.Persistence.Configurations;

public sealed class RoomConfiguration : AggregateRootConfiguration<Room>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("Rooms");

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.NormalizedName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(100);
        builder.Property(x => x.NormalizedCode).HasMaxLength(100);
        builder.Property(x => x.Capacity);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.IsActive).IsRequired();

        builder.HasOne(x => x.Clinic)
            .WithMany()
            .HasForeignKey(x => x.ClinicId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ClinicId, x.NormalizedName })
            .IsUnique()
            .HasDatabaseName("IX_Rooms_ClinicId_NormalizedName");

        builder.HasIndex(x => new { x.ClinicId, x.NormalizedCode })
            .IsUnique()
            .HasFilter("[NormalizedCode] IS NOT NULL")
            .HasDatabaseName("IX_Rooms_ClinicId_NormalizedCode");

        builder.HasIndex(x => x.ClinicId)
            .HasDatabaseName("IX_Rooms_ClinicId");
    }
}