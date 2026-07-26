using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class FundingRequestConfiguration : IEntityTypeConfiguration<FundingRequest>
{
    public void Configure(EntityTypeBuilder<FundingRequest> builder)
    {
        builder.ToTable("FundingRequests");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.AmountSar).HasPrecision(16, 2);
        builder.Property(r => r.ApprovedAmount).HasPrecision(16, 2);
        builder.Property(r => r.JustificationAr).IsRequired();
        builder.Property(r => r.JustificationEn).IsRequired();

        builder.HasOne(r => r.Idea)
            .WithMany()
            .HasForeignKey(r => r.IdeaId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.FundingStatus)
            .WithMany()
            .HasForeignKey(r => r.FundingStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Approver)
            .WithMany()
            .HasForeignKey(r => r.ApproverId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
