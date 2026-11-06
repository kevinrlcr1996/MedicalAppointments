using MedicalAppointments.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalAppointments.Infrastructure.Persistence.Configurations;

public sealed class DoctorScheduleConfiguration : AggregateRootConfiguration<DoctorSchedule>
{
    protected override void ConfigureEntity(EntityTypeBuilder<DoctorSchedule> builder)
    {
        builder.ToTable("DoctorSchedules");

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.IsDefault).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.EffectiveFrom).IsRequired();
        builder.Property(x => x.EffectiveTo);
        builder.Property(x => x.SlotDurationMinutes).IsRequired();
        builder.Property(x => x.BufferMinutes).IsRequired();

        builder.HasIndex(x => new { x.DoctorId, x.ClinicId, x.IsActive })
            .HasDatabaseName("IX_DoctorSchedules_DoctorId_ClinicId_IsActive");

        builder.HasIndex(x => x.ClinicId)
            .HasDatabaseName("IX_DoctorSchedules_ClinicId");

        builder.HasMany(x => x.Days)
            .WithOne(x => x.DoctorSchedule)
            .HasForeignKey(x => x.DoctorScheduleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}