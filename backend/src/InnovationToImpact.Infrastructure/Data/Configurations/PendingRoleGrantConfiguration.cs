using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class PendingRoleGrantConfiguration : IEntityTypeConfiguration<PendingRoleGrant>
{
    public void Configure(EntityTypeBuilder<PendingRoleGrant> builder)
    {
        builder.ToTable("PendingRoleGrants");
        builder.HasKey(g => g.Id);
        builder.Property(g => g.SamAccountName).IsRequired().HasMaxLength(100);
        builder.HasIndex(g => new { g.SamAccountName, g.RoleId }).IsUnique();

        builder.HasOne(g => g.Role)
            .WithMany()
            .HasForeignKey(g => g.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(g => g.GrantedBy)
            .WithMany()
            .HasForeignKey(g => g.GrantedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
