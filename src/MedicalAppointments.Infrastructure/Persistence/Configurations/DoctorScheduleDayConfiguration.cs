using MedicalAppointments.Domain.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalAppointments.Infrastructure.Persistence.Configurations;

public sealed class DoctorScheduleDayConfiguration : IEntityTypeConfiguration<DoctorScheduleDay>
{
    public void Configure(EntityTypeBuilder<DoctorScheduleDay> builder)
    {
        builder.ToTable("DoctorScheduleDays");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DayOfWeek).IsRequired();
        builder.Property(x => x.StartTime).IsRequired();
        builder.Property(x => x.EndTime).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => new { x.DoctorScheduleId, x.DayOfWeek })
            .IsUnique()
            .HasDatabaseName("IX_DoctorScheduleDays_DoctorScheduleId_DayOfWeek");
    }
}