using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class IpSignatureConfiguration : IEntityTypeConfiguration<IpSignature>
{
    public void Configure(EntityTypeBuilder<IpSignature> builder)
    {
        builder.ToTable("IpSignatures");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.IpTermsVersion).IsRequired().HasMaxLength(20);

        builder.HasIndex(s => new { s.IdeaId, s.UserId }).IsUnique();

        builder.HasOne(s => s.Idea)
            .WithMany()
            .HasForeignKey(s => s.IdeaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
