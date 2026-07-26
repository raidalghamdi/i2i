using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class HomePageSectionConfiguration : IEntityTypeConfiguration<HomePageSection>
{
    public void Configure(EntityTypeBuilder<HomePageSection> builder)
    {
        builder.ToTable("HomePageSections");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Type).IsRequired().HasMaxLength(50);
        builder.Property(s => s.ContentJson).IsRequired();

        builder.HasIndex(s => s.Idx);
    }
}
