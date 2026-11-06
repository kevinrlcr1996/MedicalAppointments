using MedicalAppointments.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalAppointments.Infrastructure.Persistence.Configurations;

public sealed class ScheduleExceptionConfiguration : AggregateRootConfiguration<ScheduleException>
{
    protected override void ConfigureEntity(EntityTypeBuilder<ScheduleException> builder)
    {
        builder.ToTable("ScheduleExceptions");

        builder.Property(x => x.StartDate).IsRequired();
        builder.Property(x => x.EndDate).IsRequired();
        builder.Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(1000);
        builder.Property(x => x.IsActive).IsRequired();

        builder.HasIndex(x => new { x.DoctorId, x.ClinicId, x.StartDate, x.EndDate })
            .HasDatabaseName("IX_ScheduleExceptions_DoctorId_ClinicId_StartDate_EndDate");

        builder.HasIndex(x => x.IsActive)
            .HasDatabaseName("IX_ScheduleExceptions_IsActive");
    }
}