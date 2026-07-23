using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class IdeaConfigurationTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public IdeaConfigurationTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    private static (Guid submitterId, Guid themeId, Guid statusId) SeedPrerequisites(InnovationDbContext context, string suffix)
    {
        var submitterId = Guid.NewGuid();
        var themeId = Guid.NewGuid();

        context.Users.Add(new User { Id = submitterId, SamAccountName = $"submitter-{suffix}", Email = $"{suffix}@gac-demo.sa", FullNameAr = "أ", FullNameEn = "Submitter" });
        context.StrategicThemes.Add(new StrategicTheme { Id = themeId, NameAr = "محور", NameEn = "Theme", OwnerId = submitterId });
        context.SaveChanges();

        var statusId = context.IdeaStatuses.Single(s => s.Code == "draft").Id;
        return (submitterId, themeId, statusId);
    }

    [Fact]
    public void SavesIdeaWithRequiredRelationships()
    {
        Guid ideaId;

        using (var context = _fixture.CreateContext())
        {
            var (submitterId, themeId, statusId) = SeedPrerequisites(context, "idea-t6a");

            var idea = new Idea
            {
                Id = Guid.NewGuid(),
                Code = "IDEA-0001",
                TitleAr = "فكرة تجريبية",
                TitleEn = "Sample Idea",
                ProblemStatementAr = "مشكلة",
                ProblemStatementEn = "Problem",
                ProposedSolutionAr = "حل",
                ProposedSolutionEn = "Solution",
                ExpectedBenefitsAr = "منافع",
                ExpectedBenefitsEn = "Benefits",
                StrategicThemeId = themeId,
                IdeaStatusId = statusId,
                CurrentStage = 1,
                SubmitterId = submitterId,
            };
            ideaId = idea.Id;

            context.Ideas.Add(idea);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var idea = context.Ideas.Single(i => i.Id == ideaId);
            Assert.Equal("IDEA-0001", idea.Code);
            Assert.Equal(1, idea.CurrentStage);
        }
    }

    [Fact]
    public void RejectsDuplicateIdeaCode()
    {
        using var context = _fixture.CreateContext();
        var (submitterId, themeId, statusId) = SeedPrerequisites(context, "idea-t6b");

        context.Ideas.Add(new Idea
        {
            Id = Guid.NewGuid(), Code = "IDEA-DUP", TitleAr = "أ", TitleEn = "A",
            ProblemStatementAr = "أ", ProblemStatementEn = "A", ProposedSolutionAr = "أ", ProposedSolutionEn = "A",
            ExpectedBenefitsAr = "أ", ExpectedBenefitsEn = "A",
            StrategicThemeId = themeId, IdeaStatusId = statusId, CurrentStage = 1, SubmitterId = submitterId,
        });
        context.SaveChanges();

        context.Ideas.Add(new Idea
        {
            Id = Guid.NewGuid(), Code = "IDEA-DUP", TitleAr = "ب", TitleEn = "B",
            ProblemStatementAr = "ب", ProblemStatementEn = "B", ProposedSolutionAr = "ب", ProposedSolutionEn = "B",
            ExpectedBenefitsAr = "ب", ExpectedBenefitsEn = "B",
            StrategicThemeId = themeId, IdeaStatusId = statusId, CurrentStage = 1, SubmitterId = submitterId,
        });

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }

    [Fact]
    public void RejectsCurrentStageOutOfRange()
    {
        using var context = _fixture.CreateContext();
        var (submitterId, themeId, statusId) = SeedPrerequisites(context, "idea-t6c");

        context.Ideas.Add(new Idea
        {
            Id = Guid.NewGuid(), Code = "IDEA-OOR", TitleAr = "أ", TitleEn = "A",
            ProblemStatementAr = "أ", ProblemStatementEn = "A", ProposedSolutionAr = "أ", ProposedSolutionEn = "A",
            ExpectedBenefitsAr = "أ", ExpectedBenefitsEn = "A",
            StrategicThemeId = themeId, IdeaStatusId = statusId, CurrentStage = 9, SubmitterId = submitterId,
        });

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }
}
