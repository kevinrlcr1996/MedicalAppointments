using MedicalAppointments.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalAppointments.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TokenHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.CreatedByIpAddress)
            .HasMaxLength(64);

        builder.Property(x => x.CreatedByUserAgent)
            .HasMaxLength(512);

        builder.Property(x => x.RevokedByIpAddress)
            .HasMaxLength(64);

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.HasIndex(x => x.TokenHash)
            .IsUnique()
            .HasDatabaseName("IX_RefreshTokens_TokenHash");

        builder.HasIndex(x => new { x.UserId, x.RevokedAt, x.ExpiresAt })
            .HasDatabaseName("IX_RefreshTokens_UserId_RevokedAt_ExpiresAt");

        builder.HasIndex(x => x.FamilyId)
            .HasDatabaseName("IX_RefreshTokens_FamilyId");

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}