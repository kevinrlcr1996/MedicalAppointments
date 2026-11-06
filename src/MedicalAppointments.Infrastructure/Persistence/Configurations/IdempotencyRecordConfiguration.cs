using MedicalAppointments.Domain.Idempotency;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalAppointments.Infrastructure.Persistence.Configurations;

public sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("IdempotencyKeys");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Key)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.Operation)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.RequestHash)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.ResponsePayload)
            .IsRequired(false);

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.HasIndex(x => x.Key)
            .IsUnique()
            .HasDatabaseName("IX_IdempotencyKeys_Key");

        builder.HasIndex(x => new { x.Operation, x.Status })
            .HasDatabaseName("IX_IdempotencyKeys_Operation_Status");

        builder.HasIndex(x => x.ExpiresAt)
            .HasDatabaseName("IX_IdempotencyKeys_ExpiresAt");
    }
}