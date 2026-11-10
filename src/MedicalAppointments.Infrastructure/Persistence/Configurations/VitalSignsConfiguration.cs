using MedicalAppointments.Domain.ClinicalRecords;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalAppointments.Infrastructure.Persistence.Configurations;

public sealed class VitalSignsConfiguration : AggregateRootConfiguration<VitalSigns>
{
    protected override void ConfigureEntity(EntityTypeBuilder<VitalSigns> builder)
    {
        builder.ToTable("VitalSigns");

        builder.Property(x => x.AuthorDisplayName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Note).HasMaxLength(2000);
        builder.Property(x => x.TemperatureMethod).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.Position).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.TemperatureC).HasPrecision(5, 2);
        builder.Property(x => x.WeightKg).HasPrecision(6, 2);
        builder.Property(x => x.HeightCm).HasPrecision(6, 2);

        // Direccionalidad rule 6: el vital conoce la cita; la cita NO navega a vitales.
        builder.HasOne(x => x.Appointment)
            .WithMany()
            .HasForeignKey(x => x.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AuthorDoctor)
            .WithMany()
            .HasForeignKey(x => x.AuthorDoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CorrectsVitalSigns)
            .WithMany()
            .HasForeignKey(x => x.CorrectsVitalSignsId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.AppointmentId).HasDatabaseName("IX_VitalSigns_AppointmentId");
        builder.HasIndex(x => new { x.AppointmentId, x.CreatedAt }).HasDatabaseName("IX_VitalSigns_AppointmentId_CreatedAt");
        builder.HasIndex(x => x.AuthorDoctorId).HasDatabaseName("IX_VitalSigns_AuthorDoctorId");
        builder.HasIndex(x => x.CorrectsVitalSignsId).HasDatabaseName("IX_VitalSigns_CorrectsVitalSignsId");
    }
}