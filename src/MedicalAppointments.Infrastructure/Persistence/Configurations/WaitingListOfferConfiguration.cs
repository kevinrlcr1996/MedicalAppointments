using MedicalAppointments.Domain.WaitingList;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalAppointments.Infrastructure.Persistence.Configurations;

public sealed class WaitingListOfferConfiguration : AggregateRootConfiguration<WaitingListOffer>
{
    protected override void ConfigureEntity(EntityTypeBuilder<WaitingListOffer> builder)
    {
        builder.ToTable("WaitingListOffers");

        builder.Property(x => x.LocalDate).IsRequired();
        builder.Property(x => x.SlotStartUtc).IsRequired();
        builder.Property(x => x.SlotEndUtc).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.ExpiresAt).IsRequired();

        builder.HasOne(x => x.Entry).WithMany().HasForeignKey(x => x.EntryId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Patient).WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Doctor).WithMany().HasForeignKey(x => x.DoctorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Clinic).WithMany().HasForeignKey(x => x.ClinicId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Room).WithMany().HasForeignKey(x => x.RoomId).OnDelete(DeleteBehavior.Restrict);

        // Anti-bucle: un candidato recibe a lo sumo UNA oferta por slot.
        builder.HasIndex(x => new { x.EntryId, x.SlotStartUtc })
            .IsUnique()
            .HasDatabaseName("IX_WaitingListOffers_EntryId_SlotStartUtc");

        builder.HasIndex(x => new { x.Status, x.ExpiresAt }).HasDatabaseName("IX_WaitingListOffers_Status_ExpiresAt");
        builder.HasIndex(x => new { x.PatientId, x.Status }).HasDatabaseName("IX_WaitingListOffers_PatientId_Status");
    }
}