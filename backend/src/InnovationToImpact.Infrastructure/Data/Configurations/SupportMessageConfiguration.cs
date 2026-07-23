using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class SupportMessageConfiguration : IEntityTypeConfiguration<SupportMessage>
{
    public void Configure(EntityTypeBuilder<SupportMessage> builder)
    {
        builder.ToTable("SupportMessages");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name).HasMaxLength(200);
        builder.Property(m => m.Email).HasMaxLength(200);
        builder.Property(m => m.Subject).HasMaxLength(200);
        builder.Property(m => m.Body).IsRequired();
    }
}
