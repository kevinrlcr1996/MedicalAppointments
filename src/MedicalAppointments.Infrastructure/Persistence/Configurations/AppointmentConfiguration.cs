using MedicalAppointments.Domain.Appointments;
using MedicalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalAppointments.Infrastructure.Persistence.Configurations;

public sealed class AppointmentConfiguration : AggregateRootConfiguration<Appointment>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments");

        builder.Property(x => x.LocalDate).IsRequired();
        builder.Property(x => x.StartAt).IsRequired();
        builder.Property(x => x.EndAt).IsRequired();
        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Property(x => x.CancellationReason).HasMaxLength(500);

        builder.HasOne(x => x.Patient)
            .WithMany()
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Doctor)
            .WithMany()
            .HasForeignKey(x => x.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Clinic)
            .WithMany()
            .HasForeignKey(x => x.ClinicId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Room)
            .WithMany()
            .HasForeignKey(x => x.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.DoctorId, x.StartAt, x.EndAt })
            .HasDatabaseName("IX_Appointments_DoctorId_StartAt_EndAt");

        builder.HasIndex(x => new { x.PatientId, x.StartAt })
            .HasDatabaseName("IX_Appointments_PatientId_StartAt");

        builder.HasIndex(x => new { x.ClinicId, x.StartAt })
            .HasDatabaseName("IX_Appointments_ClinicId_StartAt");

        builder.HasIndex(x => new { x.RoomId, x.StartAt, x.EndAt })
            .HasDatabaseName("IX_Appointments_RoomId_StartAt_EndAt");

        builder.HasIndex(x => new { x.Status, x.StartAt })
            .HasDatabaseName("IX_Appointments_Status_StartAt");

        builder.HasIndex(x => x.OriginalAppointmentId)
            .HasDatabaseName("IX_Appointments_OriginalAppointmentId");
    }
}