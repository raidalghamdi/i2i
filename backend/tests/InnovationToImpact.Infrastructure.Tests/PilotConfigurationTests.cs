using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class PilotConfigurationTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public PilotConfigurationTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    private static (Guid ideaId, Guid plannedStatusId) SeedPrerequisites(InnovationDbContext context, string suffix)
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
            CurrentStage = 5,
            SubmitterId = submitterId,
        });
        context.SaveChanges();

        var plannedStatusId = context.PilotStatuses.Single(s => s.Code == "planned").Id;
        return (ideaId, plannedStatusId);
    }

    [Fact]
    public void SavesPilotWithRequiredRelationships()
    {
        Guid pilotId;

        using (var context = _fixture.CreateContext())
        {
            var (ideaId, plannedStatusId) = SeedPrerequisites(context, "pilot-t2a");

            var pilot = new Pilot
            {
                Id = Guid.NewGuid(),
                IdeaId = ideaId,
                HypothesisAr = "فرضية",
                HypothesisEn = "Hypothesis",
                ExperimentPlanAr = "خطة",
                ExperimentPlanEn = "Plan",
                Budget = 50000.00m,
                StartDate = new DateTime(2026, 8, 1),
                PilotStatusId = plannedStatusId,
            };
            pilotId = pilot.Id;

            context.Pilots.Add(pilot);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var pilot = context.Pilots
                .Include(p => p.PilotStatus)
                .Single(p => p.Id == pilotId);
            Assert.Equal("planned", pilot.PilotStatus.Code);
            Assert.Equal(50000.00m, pilot.Budget);
            Assert.Null(pilot.ResultsAr);
        }
    }

    [Fact]
    public void RejectsPilotStatusIdThatDoesNotExist()
    {
        using var context = _fixture.CreateContext();
        var (ideaId, _) = SeedPrerequisites(context, "pilot-t2b");

        context.Pilots.Add(new Pilot
        {
            Id = Guid.NewGuid(),
            IdeaId = ideaId,
            HypothesisAr = "أ", HypothesisEn = "A",
            ExperimentPlanAr = "أ", ExperimentPlanEn = "A",
            Budget = 1000.00m,
            StartDate = DateTime.UtcNow,
            PilotStatusId = Guid.NewGuid(),
        });

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }
}
