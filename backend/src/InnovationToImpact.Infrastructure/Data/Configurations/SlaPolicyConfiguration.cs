using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class SlaPolicyConfiguration : IEntityTypeConfiguration<SlaPolicy>
{
    public void Configure(EntityTypeBuilder<SlaPolicy> builder)
    {
        builder.ToTable("SlaPolicies");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.EntityType).IsRequired().HasMaxLength(100);
        builder.Property(p => p.FromState).IsRequired().HasMaxLength(100);
        builder.Property(p => p.ToState).IsRequired().HasMaxLength(100);

        builder.HasIndex(p => new { p.EntityType, p.FromState, p.ToState }).IsUnique();

        builder.HasData(
            new SlaPolicy
            {
                Id = new Guid("00000000-0000-0000-0027-000000000001"),
                EntityType = "evaluation",
                FromState = "evaluation",
                ToState = "evaluated",
                TargetHours = 72,
                WarnAtPct = 80,
            },
            new SlaPolicy
            {
                Id = new Guid("00000000-0000-0000-0027-000000000002"),
                EntityType = "committee",
                FromState = "committee",
                ToState = "decided",
                TargetHours = 168,
                WarnAtPct = 80,
            },
            new SlaPolicy
            {
                Id = new Guid("00000000-0000-0000-0029-000000000001"),
                EntityType = "assignment",
                FromState = "assignment",
                ToState = "completed",
                TargetHours = 72,
                WarnAtPct = 80,
            });
    }
}
