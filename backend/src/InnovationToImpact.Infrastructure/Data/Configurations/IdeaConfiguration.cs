using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InnovationToImpact.Infrastructure.Data.Configurations;

public class IdeaConfiguration : IEntityTypeConfiguration<Idea>
{
    public void Configure(EntityTypeBuilder<Idea> builder)
    {
        builder.ToTable("Ideas", t => t.HasCheckConstraint("CK_Ideas_CurrentStage", "[CurrentStage] >= 0 AND [CurrentStage] <= 8"));
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(i => i.Code).IsUnique();

        builder.Property(i => i.TitleAr).IsRequired().HasMaxLength(500);
        builder.Property(i => i.TitleEn).IsRequired().HasMaxLength(500);
        builder.Property(i => i.ProblemStatementAr).IsRequired();
        builder.Property(i => i.ProblemStatementEn).IsRequired();
        builder.Property(i => i.ProposedSolutionAr).IsRequired();
        builder.Property(i => i.ProposedSolutionEn).IsRequired();
        builder.Property(i => i.ExpectedBenefitsAr).IsRequired();
        builder.Property(i => i.ExpectedBenefitsEn).IsRequired();

        builder.Property(i => i.CommitteeFinalScore).HasPrecision(5, 2);
        builder.Property(i => i.ScreeningReason).HasMaxLength(1000);
        builder.Property(i => i.ParticipationType).IsRequired().HasMaxLength(20).HasDefaultValue("individual");
        builder.Property(i => i.TeamName).HasMaxLength(255);
        builder.Property(i => i.EditableSections).HasMaxLength(500);

        builder.HasOne(i => i.StrategicTheme)
            .WithMany()
            .HasForeignKey(i => i.StrategicThemeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Activity)
            .WithMany()
            .HasForeignKey(i => i.ActivityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Challenge)
            .WithMany()
            .HasForeignKey(i => i.ChallengeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.IdeaStatus)
            .WithMany()
            .HasForeignKey(i => i.IdeaStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Submitter)
            .WithMany()
            .HasForeignKey(i => i.SubmitterId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
