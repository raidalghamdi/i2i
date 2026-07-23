using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class FundingRequestConfigurationTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public FundingRequestConfigurationTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    private static (Guid ideaId, Guid pendingStatusId) SeedPrerequisites(InnovationDbContext context, string suffix)
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
            CurrentStage = 7,
            SubmitterId = submitterId,
        });
        context.SaveChanges();

        var pendingStatusId = context.FundingStatuses.Single(s => s.Code == "pending").Id;
        return (ideaId, pendingStatusId);
    }

    [Fact]
    public void SavesFundingRequestWithRequiredRelationships()
    {
        Guid requestId;

        using (var context = _fixture.CreateContext())
        {
            var (ideaId, pendingStatusId) = SeedPrerequisites(context, "fund-t4a");

            var request = new FundingRequest
            {
                Id = Guid.NewGuid(),
                IdeaId = ideaId,
                AmountSar = 250000.00m,
                JustificationAr = "مبرر",
                JustificationEn = "Justification",
                FundingStatusId = pendingStatusId,
            };
            requestId = request.Id;

            context.FundingRequests.Add(request);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var request = context.FundingRequests
                .Include(r => r.FundingStatus)
                .Single(r => r.Id == requestId);
            Assert.Equal("pending", request.FundingStatus.Code);
            Assert.Equal(250000.00m, request.AmountSar);
        }
    }

    [Fact]
    public void AllowsNullApproverForAPendingRequest()
    {
        using var context = _fixture.CreateContext();
        var (ideaId, pendingStatusId) = SeedPrerequisites(context, "fund-t4b");

        context.FundingRequests.Add(new FundingRequest
        {
            Id = Guid.NewGuid(),
            IdeaId = ideaId,
            AmountSar = 10000.00m,
            JustificationAr = "أ",
            JustificationEn = "A",
            FundingStatusId = pendingStatusId,
            ApproverId = null,
        });

        var exception = Record.Exception(() => context.SaveChanges());
        Assert.Null(exception);
    }
}
