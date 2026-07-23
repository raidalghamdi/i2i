using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class EvaluationConfigurationTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public EvaluationConfigurationTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    private static (Guid ideaId, Guid evaluatorId) SeedPrerequisites(InnovationDbContext context, string suffix)
    {
        var submitterId = Guid.NewGuid();
        var evaluatorId = Guid.NewGuid();
        var themeId = Guid.NewGuid();

        context.Users.Add(new User { Id = submitterId, SamAccountName = $"submitter-{suffix}", Email = $"sub-{suffix}@gac-demo.sa", FullNameAr = "أ", FullNameEn = "Submitter" });
        context.Users.Add(new User { Id = evaluatorId, SamAccountName = $"evaluator-{suffix}", Email = $"eval-{suffix}@gac-demo.sa", FullNameAr = "ب", FullNameEn = "Evaluator" });
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
            CurrentStage = 3,
            SubmitterId = submitterId,
        });
        context.SaveChanges();

        return (ideaId, evaluatorId);
    }

    [Fact]
    public void SavesEvaluationWithRequiredRelationships()
    {
        Guid evaluationId;

        using (var context = _fixture.CreateContext())
        {
            var (ideaId, evaluatorId) = SeedPrerequisites(context, "eval-t2a");

            var evaluation = new Evaluation
            {
                Id = Guid.NewGuid(),
                IdeaId = ideaId,
                EvaluatorId = evaluatorId,
                CriteriaScoresJson = "{\"innovation\":8,\"impact\":7}",
                TotalScore = 7.5m,
                ConflictOfInterest = false,
            };
            evaluationId = evaluation.Id;

            context.Evaluations.Add(evaluation);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var evaluation = context.Evaluations.Single(e => e.Id == evaluationId);
            Assert.Equal(7.5m, evaluation.TotalScore);
            Assert.False(evaluation.ConflictOfInterest);
        }
    }

    [Fact]
    public void RejectsDuplicateEvaluationForSameIdeaAndEvaluator()
    {
        using var context = _fixture.CreateContext();
        var (ideaId, evaluatorId) = SeedPrerequisites(context, "eval-t2b");

        context.Evaluations.Add(new Evaluation { Id = Guid.NewGuid(), IdeaId = ideaId, EvaluatorId = evaluatorId, CriteriaScoresJson = "{}", TotalScore = 5m });
        context.SaveChanges();

        context.Evaluations.Add(new Evaluation { Id = Guid.NewGuid(), IdeaId = ideaId, EvaluatorId = evaluatorId, CriteriaScoresJson = "{}", TotalScore = 6m });

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }
}
