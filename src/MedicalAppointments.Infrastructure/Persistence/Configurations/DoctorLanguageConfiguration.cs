using MedicalAppointments.Domain.MedicalStructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MedicalAppointments.Infrastructure.Persistence.Configurations;

public sealed class DoctorLanguageConfiguration : IEntityTypeConfiguration<DoctorLanguage>
{
    public void Configure(EntityTypeBuilder<DoctorLanguage> builder)
    {
        builder.ToTable("DoctorLanguages");

        builder.HasKey(x => new { x.DoctorId, x.Language });

        builder.Property(x => x.Language)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.AddedAt).IsRequired();

        builder.HasOne(x => x.Doctor)
            .WithMany(x => x.Languages)
            .HasForeignKey(x => x.DoctorId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}