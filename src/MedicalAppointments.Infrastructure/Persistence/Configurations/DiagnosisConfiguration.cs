using MedicalAppointments.Domain.ClinicalRecords;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalAppointments.Infrastructure.Persistence.Configurations;

public sealed class DiagnosisConfiguration : AggregateRootConfiguration<Diagnosis>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Diagnosis> builder)
    {
        builder.ToTable("Diagnoses");

        builder.Property(x => x.AuthorDisplayName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(10).IsRequired();
        builder.Property(x => x.DisplayTerm).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Category).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Note).HasMaxLength(2000);

        // Rule 6: el diagnóstico conoce la cita; la cita NO navega a diagnósticos.
        builder.HasOne(x => x.Appointment)
            .WithMany()
            .HasForeignKey(x => x.AppointmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AuthorDoctor)
            .WithMany()
            .HasForeignKey(x => x.AuthorDoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CorrectsDiagnosis)
            .WithMany()
            .HasForeignKey(x => x.CorrectsDiagnosisId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.AppointmentId).HasDatabaseName("IX_Diagnoses_AppointmentId");
        builder.HasIndex(x => new { x.AppointmentId, x.CreatedAt }).HasDatabaseName("IX_Diagnoses_AppointmentId_CreatedAt");
        builder.HasIndex(x => x.AuthorDoctorId).HasDatabaseName("IX_Diagnoses_AuthorDoctorId");
        builder.HasIndex(x => x.CorrectsDiagnosisId).HasDatabaseName("IX_Diagnoses_CorrectsDiagnosisId");
        builder.HasIndex(x => x.Code).HasDatabaseName("IX_Diagnoses_Code");
    }
}