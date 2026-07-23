using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class CommitteeDecisionConfigurationTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public CommitteeDecisionConfigurationTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    private static (Guid ideaId, Guid deciderId, Guid approvedTypeId) SeedPrerequisites(InnovationDbContext context, string suffix)
    {
        var submitterId = Guid.NewGuid();
        var deciderId = Guid.NewGuid();
        var themeId = Guid.NewGuid();

        context.Users.Add(new User { Id = submitterId, SamAccountName = $"submitter-{suffix}", Email = $"sub-{suffix}@gac-demo.sa", FullNameAr = "أ", FullNameEn = "Submitter" });
        context.Users.Add(new User { Id = deciderId, SamAccountName = $"judge-{suffix}", Email = $"judge-{suffix}@gac-demo.sa", FullNameAr = "ب", FullNameEn = "Judge" });
        context.StrategicThemes.Add(new StrategicTheme { Id = themeId, NameAr = "محور", NameEn = "Theme", OwnerId = submitterId });
        context.SaveChanges();

        var statusId = context.IdeaStatuses.Single(s => s.Code == "draft").Id;
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
            IdeaStatusId = statusId,
            CurrentStage = 4,
            SubmitterId = submitterId,
        });
        context.SaveChanges();

        var approvedTypeId = context.CommitteeDecisionTypes.Single(t => t.Code == "approved").Id;
        return (ideaId, deciderId, approvedTypeId);
    }

    [Fact]
    public void SavesCommitteeDecisionWithRequiredRelationships()
    {
        Guid decisionId;

        using (var context = _fixture.CreateContext())
        {
            var (ideaId, deciderId, approvedTypeId) = SeedPrerequisites(context, "cd-t6a");

            var decision = new CommitteeDecision
            {
                Id = Guid.NewGuid(),
                IdeaId = ideaId,
                CommitteeName = "Innovation Committee Q3",
                CommitteeDecisionTypeId = approvedTypeId,
                QuorumMet = true,
                DecidedAt = DateTime.UtcNow,
                DecidedById = deciderId,
            };
            decisionId = decision.Id;

            context.CommitteeDecisions.Add(decision);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var decision = context.CommitteeDecisions
                .Include(d => d.CommitteeDecisionType)
                .Single(d => d.Id == decisionId);
            Assert.Equal("approved", decision.CommitteeDecisionType.Code);
            Assert.True(decision.QuorumMet);
        }
    }

    [Fact]
    public void AllowsNullDecidedByForAPendingDecision()
    {
        using var context = _fixture.CreateContext();
        var (ideaId, _, approvedTypeId) = SeedPrerequisites(context, "cd-t6b");

        context.CommitteeDecisions.Add(new CommitteeDecision
        {
            Id = Guid.NewGuid(),
            IdeaId = ideaId,
            CommitteeName = "Innovation Committee Q4",
            CommitteeDecisionTypeId = approvedTypeId,
            QuorumMet = false,
            DecidedById = null,
        });

        var exception = Record.Exception(() => context.SaveChanges());
        Assert.Null(exception);
    }
}
