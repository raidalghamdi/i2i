using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class ApprovalStepDecisionConfiguration : IEntityTypeConfiguration<ApprovalStepDecision>
{
    public void Configure(EntityTypeBuilder<ApprovalStepDecision> builder)
    {
        builder.ToTable("ApprovalStepDecisions");
        builder.HasKey(d => d.Id);

        builder.HasOne(d => d.ApprovalInstance)
            .WithMany()
            .HasForeignKey(d => d.ApprovalInstanceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.ApprovalChainStep)
            .WithMany()
            .HasForeignKey(d => d.ApprovalChainStepId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Decider)
            .WithMany()
            .HasForeignKey(d => d.DeciderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.ApprovalDecisionType)
            .WithMany()
            .HasForeignKey(d => d.ApprovalDecisionTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
