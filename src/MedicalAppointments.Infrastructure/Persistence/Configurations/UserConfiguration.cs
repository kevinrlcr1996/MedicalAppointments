using MedicalAppointments.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalAppointments.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(x => x.NormalizedEmail)
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(x => x.PasswordHash)
            .HasMaxLength(1024)
            .IsRequired();

        builder.Property(x => x.FirstName)
            .HasMaxLength(200);

        builder.Property(x => x.LastName)
            .HasMaxLength(200);

        builder.Property(x => x.EmailConfirmationTokenHash)
            .HasMaxLength(128);

        builder.Property(x => x.PasswordResetTokenHash)
            .HasMaxLength(128);

        builder.Property(x => x.SecurityStamp)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.HasIndex(x => x.NormalizedEmail)
            .IsUnique()
            .HasDatabaseName("IX_Users_NormalizedEmail");

        builder.HasIndex(x => x.EmailConfirmationTokenHash)
            .IsUnique()
            .HasFilter("[EmailConfirmationTokenHash] IS NOT NULL")
            .HasDatabaseName("IX_Users_EmailConfirmationTokenHash");

        builder.HasIndex(x => x.PasswordResetTokenHash)
            .IsUnique()
            .HasFilter("[PasswordResetTokenHash] IS NOT NULL")
            .HasDatabaseName("IX_Users_PasswordResetTokenHash");

        builder.HasMany(x => x.Roles)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.DirectPermissions)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}