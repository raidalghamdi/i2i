using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class ApprovalChainConfiguration : IEntityTypeConfiguration<ApprovalChain>
{
    public void Configure(EntityTypeBuilder<ApprovalChain> builder)
    {
        builder.ToTable("ApprovalChains");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code).IsRequired().HasMaxLength(100);
        builder.HasIndex(c => c.Code).IsUnique();

        builder.Property(c => c.NameAr).IsRequired().HasMaxLength(200);
        builder.Property(c => c.NameEn).IsRequired().HasMaxLength(200);
        builder.Property(c => c.EntityType).IsRequired().HasMaxLength(100);

        builder.Property(c => c.IsActive).HasDefaultValue(true);

        builder.HasData(
            new ApprovalChain
            {
                Id = new Guid("00000000-0000-0000-0032-000000000001"),
                Code = "committee-publish",
                NameAr = "نشر قرار اللجنة",
                NameEn = "Committee publish",
                EntityType = "committee_decision",
                IsActive = true,
            },
            new ApprovalChain
            {
                Id = new Guid("00000000-0000-0000-0032-000000000002"),
                Code = "idea-approve",
                NameAr = "اعتماد الفكرة",
                NameEn = "Idea approval",
                EntityType = "idea",
                IsActive = true,
            });
    }
}
