using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class KnowledgeArticleConfiguration : IEntityTypeConfiguration<KnowledgeArticle>
{
    public void Configure(EntityTypeBuilder<KnowledgeArticle> builder)
    {
        builder.ToTable("KnowledgeArticles");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.TitleAr).IsRequired().HasMaxLength(500);
        builder.Property(a => a.TitleEn).IsRequired().HasMaxLength(500);
        builder.Property(a => a.ContentMdAr).IsRequired();
        builder.Property(a => a.ContentMdEn).IsRequired();
        builder.Property(a => a.SourceUrl).HasMaxLength(2000);
        builder.Property(a => a.SourceLabelAr).HasMaxLength(300);
        builder.Property(a => a.SourceLabelEn).HasMaxLength(300);

        builder.HasOne(a => a.Idea)
            .WithMany()
            .HasForeignKey(a => a.IdeaId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.KnowledgeType)
            .WithMany()
            .HasForeignKey(a => a.KnowledgeTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.KnowledgeVisibility)
            .WithMany()
            .HasForeignKey(a => a.KnowledgeVisibilityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Author)
            .WithMany()
            .HasForeignKey(a => a.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
