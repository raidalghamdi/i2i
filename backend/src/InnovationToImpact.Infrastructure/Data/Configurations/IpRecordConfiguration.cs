using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class IpRecordConfiguration : IEntityTypeConfiguration<IpRecord>
{
    public void Configure(EntityTypeBuilder<IpRecord> builder)
    {
        builder.ToTable("IpRecords");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.OwnershipPartyAr).IsRequired();
        builder.Property(r => r.OwnershipPartyEn).IsRequired();
        builder.Property(r => r.ConfidentialityTermsAr).IsRequired();
        builder.Property(r => r.ConfidentialityTermsEn).IsRequired();
        builder.Property(r => r.ParticipationConditionsAr).IsRequired();
        builder.Property(r => r.ParticipationConditionsEn).IsRequired();

        builder.HasOne(r => r.Idea)
            .WithMany()
            .HasForeignKey(r => r.IdeaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.IpType)
            .WithMany()
            .HasForeignKey(r => r.IpTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
