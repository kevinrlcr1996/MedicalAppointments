using MedicalAppointments.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalAppointments.Infrastructure.Persistence.Configurations;

public sealed class UserPermissionConfiguration : IEntityTypeConfiguration<UserPermission>
{
    public void Configure(EntityTypeBuilder<UserPermission> builder)
    {
        builder.ToTable("UserPermissions");

        builder.HasKey(x => new { x.UserId, x.PermissionId });

        builder.Property(x => x.GrantedAt)
            .IsRequired();

        builder.HasIndex(x => x.PermissionId)
            .HasDatabaseName("IX_UserPermissions_PermissionId");
    }
}