using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class ChallengeConfiguration : IEntityTypeConfiguration<Challenge>
{
    public void Configure(EntityTypeBuilder<Challenge> builder)
    {
        builder.ToTable("Challenges");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.TextAr).IsRequired();
        builder.Property(c => c.TextEn).IsRequired();

        builder.HasOne(c => c.StrategicTheme)
            .WithMany()
            .HasForeignKey(c => c.StrategicThemeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
