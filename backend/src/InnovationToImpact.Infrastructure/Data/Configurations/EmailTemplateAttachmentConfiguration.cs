using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class EmailTemplateAttachmentConfiguration : IEntityTypeConfiguration<EmailTemplateAttachment>
{
    public void Configure(EntityTypeBuilder<EmailTemplateAttachment> builder)
    {
        builder.ToTable("EmailTemplateAttachments");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.FileName).IsRequired().HasMaxLength(255);
        builder.Property(a => a.BlobPath).IsRequired().HasMaxLength(500);
        builder.Property(a => a.ContentType).IsRequired().HasMaxLength(150);

        builder.HasOne(a => a.EmailTemplate)
            .WithMany()
            .HasForeignKey(a => a.EmailTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Uploader)
            .WithMany()
            .HasForeignKey(a => a.UploaderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
