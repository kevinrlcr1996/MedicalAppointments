using MedicalAppointments.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalAppointments.Infrastructure.Persistence.Configurations;

public sealed class BlockoutConfiguration : AggregateRootConfiguration<Blockout>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Blockout> builder)
    {
        builder.ToTable("Blockouts");

        builder.Property(x => x.StartAt).IsRequired();
        builder.Property(x => x.EndAt).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(1000);
        builder.Property(x => x.IsActive).IsRequired();

        builder.HasIndex(x => new { x.DoctorId, x.StartAt, x.EndAt })
            .HasDatabaseName("IX_Blockouts_DoctorId_StartAt_EndAt");

        builder.HasIndex(x => new { x.RoomId, x.StartAt, x.EndAt })
            .HasDatabaseName("IX_Blockouts_RoomId_StartAt_EndAt");

        builder.HasIndex(x => new { x.ResourceId, x.StartAt, x.EndAt })
            .HasDatabaseName("IX_Blockouts_ResourceId_StartAt_EndAt");

        builder.HasIndex(x => x.IsActive)
            .HasDatabaseName("IX_Blockouts_IsActive");
    }
}