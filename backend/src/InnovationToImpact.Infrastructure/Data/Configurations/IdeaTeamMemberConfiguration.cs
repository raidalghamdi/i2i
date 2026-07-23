using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class IdeaTeamMemberConfiguration : IEntityTypeConfiguration<IdeaTeamMember>
{
    public void Configure(EntityTypeBuilder<IdeaTeamMember> builder)
    {
        builder.ToTable("IdeaTeamMembers");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name).IsRequired().HasMaxLength(255);
        builder.Property(m => m.Email).IsRequired().HasMaxLength(255);

        builder.HasOne(m => m.Idea)
            .WithMany(i => i.TeamMembers)
            .HasForeignKey(m => m.IdeaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
