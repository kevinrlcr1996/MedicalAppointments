using MedicalAppointments.Domain.WaitingList;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalAppointments.Infrastructure.Persistence.Configurations;

public sealed class WaitingListEntryConfiguration : AggregateRootConfiguration<WaitingListEntry>
{
    protected override void ConfigureEntity(EntityTypeBuilder<WaitingListEntry> builder)
    {
        builder.ToTable("WaitingListEntries");

        builder.Property(x => x.PreferredFromUtc).IsRequired();
        builder.Property(x => x.PreferredToUtc).IsRequired();
        builder.Property(x => x.Priority).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.Note).HasMaxLength(1000);

        builder.HasOne(x => x.Patient).WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Doctor).WithMany().HasForeignKey(x => x.DoctorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Clinic).WithMany().HasForeignKey(x => x.ClinicId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.DoctorId, x.Status }).HasDatabaseName("IX_WaitingListEntries_DoctorId_Status");
        builder.HasIndex(x => new { x.PatientId, x.Status }).HasDatabaseName("IX_WaitingListEntries_PatientId_Status");
        builder.HasIndex(x => new { x.DoctorId, x.ClinicId, x.Status, x.Priority, x.CreatedAt })
            .HasDatabaseName("IX_WaitingListEntries_Matching");
    }
}