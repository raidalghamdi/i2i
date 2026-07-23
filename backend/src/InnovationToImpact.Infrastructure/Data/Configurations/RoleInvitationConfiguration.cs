using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class RoleInvitationConfiguration : IEntityTypeConfiguration<RoleInvitation>
{
    public void Configure(EntityTypeBuilder<RoleInvitation> builder)
    {
        builder.ToTable("RoleInvitations");
        builder.HasKey(ri => ri.Id);
        builder.Property(ri => ri.SamAccountName).IsRequired().HasMaxLength(100);
        builder.Property(ri => ri.DisplayName).HasMaxLength(200);
        builder.Property(ri => ri.Email).HasMaxLength(320);
        builder.Property(ri => ri.Source).IsRequired().HasMaxLength(20);

        builder.HasIndex(ri => new { ri.SamAccountName, ri.RoleId, ri.RoleInvitationStatusId });

        builder.HasOne(ri => ri.Role)
            .WithMany()
            .HasForeignKey(ri => ri.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ri => ri.RoleInvitationStatus)
            .WithMany()
            .HasForeignKey(ri => ri.RoleInvitationStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ri => ri.InvitedBy)
            .WithMany()
            .HasForeignKey(ri => ri.InvitedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
