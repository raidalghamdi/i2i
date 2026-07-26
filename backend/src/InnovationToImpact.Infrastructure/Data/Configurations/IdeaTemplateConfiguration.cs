using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class IdeaTemplateConfiguration : IEntityTypeConfiguration<IdeaTemplate>
{
    public void Configure(EntityTypeBuilder<IdeaTemplate> builder)
    {
        builder.ToTable("IdeaTemplates");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.FileName).IsRequired().HasMaxLength(255);
        builder.Property(t => t.ContentType).IsRequired().HasMaxLength(150);
        builder.Property(t => t.StoredPath).IsRequired().HasMaxLength(500);

        builder.HasIndex(t => t.UploadedAt);
    }
}
