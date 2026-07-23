using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class ApprovalChainStepConfiguration : IEntityTypeConfiguration<ApprovalChainStep>
{
    public void Configure(EntityTypeBuilder<ApprovalChainStep> builder)
    {
        builder.ToTable("ApprovalChainSteps");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.IsRequired).HasDefaultValue(true);

        builder.Property(s => s.MinApprovers).HasDefaultValue(1);
        builder.Property(s => s.LabelAr).HasMaxLength(200).IsRequired();
        builder.Property(s => s.LabelEn).HasMaxLength(200).IsRequired();

        builder.HasIndex(s => new { s.ApprovalChainId, s.StepOrder }).IsUnique();

        builder.HasOne(s => s.ApprovalChain)
            .WithMany()
            .HasForeignKey(s => s.ApprovalChainId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Role)
            .WithMany()
            .HasForeignKey(s => s.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasData(
            new ApprovalChainStep
            {
                Id = new Guid("00000000-0000-0000-0031-000000000001"),
                ApprovalChainId = new Guid("00000000-0000-0000-0032-000000000001"),
                StepOrder = 1,
                RoleId = new Guid("00000000-0000-0000-0023-000000000004"), // evaluator
                MinApprovers = 1,
                IsRequired = true,
                LabelAr = "تقييم المُقيّم",
                LabelEn = "Evaluator scoring",
            },
            new ApprovalChainStep
            {
                Id = new Guid("00000000-0000-0000-0031-000000000002"),
                ApprovalChainId = new Guid("00000000-0000-0000-0032-000000000001"),
                StepOrder = 2,
                RoleId = new Guid("00000000-0000-0000-0023-000000000003"), // judge
                MinApprovers = 2,
                IsRequired = true,
                LabelAr = "موافقة اثنين من ثلاثة محكّمين",
                LabelEn = "2-of-3 judges approve",
            },
            new ApprovalChainStep
            {
                Id = new Guid("00000000-0000-0000-0031-000000000003"),
                ApprovalChainId = new Guid("00000000-0000-0000-0032-000000000002"),
                StepOrder = 1,
                RoleId = new Guid("00000000-0000-0000-0023-000000000001"), // admin
                MinApprovers = 1,
                IsRequired = true,
                LabelAr = "موافقة المشرف",
                LabelEn = "Admin approval",
            });
    }
}
