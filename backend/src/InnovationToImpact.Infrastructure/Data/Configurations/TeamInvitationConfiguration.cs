using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class TeamInvitationConfiguration : IEntityTypeConfiguration<TeamInvitation>
{
    public void Configure(EntityTypeBuilder<TeamInvitation> builder)
    {
        builder.ToTable("TeamInvitations");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.InvitedEmail).IsRequired().HasMaxLength(320);
        builder.Property(i => i.Token).IsRequired().HasMaxLength(200);
        builder.HasIndex(i => i.Token).IsUnique();

        builder.HasOne(i => i.Team)
            .WithMany()
            .HasForeignKey(i => i.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.InvitedBy)
            .WithMany()
            .HasForeignKey(i => i.InvitedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.TeamInvitationStatus)
            .WithMany()
            .HasForeignKey(i => i.TeamInvitationStatusId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
