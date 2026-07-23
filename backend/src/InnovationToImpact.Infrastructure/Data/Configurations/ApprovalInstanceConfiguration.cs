using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class ApprovalInstanceConfiguration : IEntityTypeConfiguration<ApprovalInstance>
{
    public void Configure(EntityTypeBuilder<ApprovalInstance> builder)
    {
        builder.ToTable("ApprovalInstances");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.EntityType).IsRequired().HasMaxLength(100);
        builder.Property(i => i.CurrentStepOrder).HasDefaultValue(1);

        // EntityId is a deliberately unconstrained polymorphic reference — no HasOne/HasForeignKey here.

        builder.HasOne(i => i.ApprovalChain)
            .WithMany()
            .HasForeignKey(i => i.ApprovalChainId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.ApprovalInstanceStatus)
            .WithMany()
            .HasForeignKey(i => i.ApprovalInstanceStatusId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
