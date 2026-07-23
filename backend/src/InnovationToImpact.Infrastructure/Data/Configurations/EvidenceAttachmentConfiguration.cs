using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class EvidenceAttachmentConfiguration : IEntityTypeConfiguration<EvidenceAttachment>
{
    public void Configure(EntityTypeBuilder<EvidenceAttachment> builder)
    {
        builder.ToTable("EvidenceAttachments");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.EntityType).IsRequired().HasMaxLength(100);
        builder.Property(e => e.FileName).IsRequired().HasMaxLength(255);
        builder.Property(e => e.BlobPath).IsRequired().HasMaxLength(500);
        builder.Property(e => e.ContentType).IsRequired().HasMaxLength(150);

        // EntityId is a deliberately unconstrained polymorphic reference — no HasOne/HasForeignKey here.

        builder.HasOne(e => e.Uploader)
            .WithMany()
            .HasForeignKey(e => e.UploaderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
