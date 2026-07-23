using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class ScaleDecisionConfigurationTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public ScaleDecisionConfigurationTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    private static (Guid ideaId, Guid scaleTypeId) SeedPrerequisites(InnovationDbContext context, string suffix)
    {
        var submitterId = Guid.NewGuid();
        var themeId = Guid.NewGuid();

        context.Users.Add(new User { Id = submitterId, SamAccountName = $"submitter-{suffix}", Email = $"sub-{suffix}@gac-demo.sa", FullNameAr = "أ", FullNameEn = "Submitter" });
        context.StrategicThemes.Add(new StrategicTheme { Id = themeId, NameAr = "محور", NameEn = "Theme", OwnerId = submitterId });
        context.SaveChanges();

        var ideaStatusId = context.IdeaStatuses.Single(s => s.Code == "draft").Id;
        var ideaId = Guid.NewGuid();
        context.Ideas.Add(new Idea
        {
            Id = ideaId,
            Code = $"IDEA-{suffix}",
            TitleAr = "فكرة", TitleEn = "Idea",
            ProblemStatementAr = "أ", ProblemStatementEn = "A",
            ProposedSolutionAr = "أ", ProposedSolutionEn = "A",
            ExpectedBenefitsAr = "أ", ExpectedBenefitsEn = "A",
            StrategicThemeId = themeId,
            IdeaStatusId = ideaStatusId,
            CurrentStage = 6,
            SubmitterId = submitterId,
        });
        context.SaveChanges();

        var scaleTypeId = context.ScaleDecisionTypes.Single(t => t.Code == "scale").Id;
        return (ideaId, scaleTypeId);
    }

    [Fact]
    public void SavesScaleDecisionWithRequiredRelationships()
    {
        Guid decisionId;

        using (var context = _fixture.CreateContext())
        {
            var (ideaId, scaleTypeId) = SeedPrerequisites(context, "scale-t2a");

            var decision = new ScaleDecision
            {
                Id = Guid.NewGuid(),
                IdeaId = ideaId,
                EvidenceOfViabilityAr = "دليل",
                EvidenceOfViabilityEn = "Evidence",
                ValueAssessmentAr = "تقييم",
                ValueAssessmentEn = "Assessment",
                RiskAssessmentAr = "مخاطر",
                RiskAssessmentEn = "Risk",
                StrategicFitScore = 8.50m,
                ScaleDecisionTypeId = scaleTypeId,
            };
            decisionId = decision.Id;

            context.ScaleDecisions.Add(decision);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var decision = context.ScaleDecisions
                .Include(d => d.ScaleDecisionType)
                .Single(d => d.Id == decisionId);
            Assert.Equal("scale", decision.ScaleDecisionType.Code);
            Assert.Equal(8.50m, decision.StrategicFitScore);
            Assert.Null(decision.DecidedById);
        }
    }

    [Fact]
    public void AllowsNullDecidedByForAPendingScaleDecision()
    {
        using var context = _fixture.CreateContext();
        var (ideaId, scaleTypeId) = SeedPrerequisites(context, "scale-t2b");

        context.ScaleDecisions.Add(new ScaleDecision
        {
            Id = Guid.NewGuid(),
            IdeaId = ideaId,
            EvidenceOfViabilityAr = "أ", EvidenceOfViabilityEn = "A",
            ValueAssessmentAr = "أ", ValueAssessmentEn = "A",
            RiskAssessmentAr = "أ", RiskAssessmentEn = "A",
            StrategicFitScore = 5.00m,
            ScaleDecisionTypeId = scaleTypeId,
            DecidedById = null,
        });

        var exception = Record.Exception(() => context.SaveChanges());
        Assert.Null(exception);
    }
}
