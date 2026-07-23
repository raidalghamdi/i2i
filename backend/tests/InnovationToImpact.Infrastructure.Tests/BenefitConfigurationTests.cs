using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class BenefitConfigurationTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public BenefitConfigurationTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    private static (Guid ideaId, Guid quantitativeTypeId, Guid financialCategoryId) SeedPrerequisites(InnovationDbContext context, string suffix)
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

        var quantitativeTypeId = context.BenefitTypes.Single(t => t.Code == "quantitative").Id;
        var financialCategoryId = context.BenefitCategories.Single(c => c.Code == "financial").Id;
        return (ideaId, quantitativeTypeId, financialCategoryId);
    }

    [Fact]
    public void SavesBenefitWithRequiredRelationships()
    {
        Guid benefitId;

        using (var context = _fixture.CreateContext())
        {
            var (ideaId, quantitativeTypeId, financialCategoryId) = SeedPrerequisites(context, "ben-t3a");

            var benefit = new Benefit
            {
                Id = Guid.NewGuid(),
                IdeaId = ideaId,
                TitleAr = "منفعة",
                TitleEn = "Cost Savings",
                BenefitTypeId = quantitativeTypeId,
                BenefitCategoryId = financialCategoryId,
                TargetValue = 100000.00m,
                MeasurementUnit = "SAR",
            };
            benefitId = benefit.Id;

            context.Benefits.Add(benefit);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var benefit = context.Benefits
                .Include(b => b.BenefitType)
                .Include(b => b.BenefitCategory)
                .Single(b => b.Id == benefitId);
            Assert.Equal("quantitative", benefit.BenefitType.Code);
            Assert.Equal("financial", benefit.BenefitCategory.Code);
            Assert.Equal(100000.00m, benefit.TargetValue);
        }
    }

    [Fact]
    public void AllowsNullVerifiedByForAnUnverifiedBenefit()
    {
        using var context = _fixture.CreateContext();
        var (ideaId, quantitativeTypeId, financialCategoryId) = SeedPrerequisites(context, "ben-t3b");

        context.Benefits.Add(new Benefit
        {
            Id = Guid.NewGuid(),
            IdeaId = ideaId,
            TitleAr = "منفعة",
            TitleEn = "Unverified Benefit",
            BenefitTypeId = quantitativeTypeId,
            BenefitCategoryId = financialCategoryId,
            VerifiedById = null,
        });

        var exception = Record.Exception(() => context.SaveChanges());
        Assert.Null(exception);
    }
}
