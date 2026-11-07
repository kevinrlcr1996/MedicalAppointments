using MedicalAppointments.Domain.WaitingRoom;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalAppointments.Infrastructure.Persistence.Configurations;

public sealed class WaitingRoomCallConfiguration : AggregateRootConfiguration<WaitingRoomCall>
{
    protected override void ConfigureEntity(EntityTypeBuilder<WaitingRoomCall> builder)
    {
        builder.ToTable("WaitingRoomCalls");

        builder.Property(x => x.LocalDate).IsRequired();
        builder.Property(x => x.SequenceNumber).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.RecallCount).IsRequired();
        builder.Property(x => x.Note).HasMaxLength(500);

        builder.HasOne(x => x.Appointment)
            .WithMany()
            .HasForeignKey(x => x.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Doctor).WithMany().HasForeignKey(x => x.DoctorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Clinic).WithMany().HasForeignKey(x => x.ClinicId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Room).WithMany().HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.AppointmentId).IsUnique().HasDatabaseName("IX_WaitingRoomCalls_AppointmentId");
        builder.HasIndex(x => new { x.DoctorId, x.ClinicId, x.LocalDate, x.SequenceNumber })
            .IsUnique()
            .HasDatabaseName("IX_WaitingRoomCalls_DoctorId_ClinicId_LocalDate_SequenceNumber");
        builder.HasIndex(x => new { x.DoctorId, x.ClinicId, x.LocalDate, x.Status })
            .HasDatabaseName("IX_WaitingRoomCalls_Board");
    }
}