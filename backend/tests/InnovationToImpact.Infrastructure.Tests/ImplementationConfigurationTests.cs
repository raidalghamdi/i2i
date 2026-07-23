using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class ImplementationConfigurationTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public ImplementationConfigurationTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    private static (Guid ideaId, Guid ownerId, Guid pendingStatusId) SeedPrerequisites(InnovationDbContext context, string suffix)
    {
        var submitterId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var themeId = Guid.NewGuid();

        context.Users.Add(new User { Id = submitterId, SamAccountName = $"submitter-{suffix}", Email = $"sub-{suffix}@gac-demo.sa", FullNameAr = "أ", FullNameEn = "Submitter" });
        context.Users.Add(new User { Id = ownerId, SamAccountName = $"owner-{suffix}", Email = $"owner-{suffix}@gac-demo.sa", FullNameAr = "ب", FullNameEn = "Owner" });
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

        var pendingStatusId = context.HandoverStatuses.Single(s => s.Code == "pending").Id;
        return (ideaId, ownerId, pendingStatusId);
    }

    [Fact]
    public void SavesImplementationWithRequiredRelationships()
    {
        Guid implementationId;
        Guid ownerId;

        using (var context = _fixture.CreateContext())
        {
            var (ideaId, extractedOwnerId, pendingStatusId) = SeedPrerequisites(context, "impl-t3a");
            ownerId = extractedOwnerId;

            var implementation = new Implementation
            {
                Id = Guid.NewGuid(),
                IdeaId = ideaId,
                OperationalOwnerId = ownerId,
                IntegrationPlanAr = "خطة",
                IntegrationPlanEn = "Plan",
                ResourceCommitmentAr = "التزام",
                ResourceCommitmentEn = "Commitment",
                HandoverStatusId = pendingStatusId,
                LineUnit = "Digital Services",
            };
            implementationId = implementation.Id;

            context.Implementations.Add(implementation);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var implementation = context.Implementations
                .Include(i => i.HandoverStatus)
                .Include(i => i.OperationalOwner)
                .Single(i => i.Id == implementationId);
            Assert.Equal("pending", implementation.HandoverStatus.Code);
            Assert.Equal(ownerId, implementation.OperationalOwner.Id);
            Assert.Equal("Digital Services", implementation.LineUnit);
        }
    }

    [Fact]
    public void RejectsHandoverStatusIdThatDoesNotExist()
    {
        using var context = _fixture.CreateContext();
        var (ideaId, ownerId, _) = SeedPrerequisites(context, "impl-t3b");

        context.Implementations.Add(new Implementation
        {
            Id = Guid.NewGuid(),
            IdeaId = ideaId,
            OperationalOwnerId = ownerId,
            IntegrationPlanAr = "أ", IntegrationPlanEn = "A",
            ResourceCommitmentAr = "أ", ResourceCommitmentEn = "A",
            HandoverStatusId = Guid.NewGuid(),
        });

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }
}
