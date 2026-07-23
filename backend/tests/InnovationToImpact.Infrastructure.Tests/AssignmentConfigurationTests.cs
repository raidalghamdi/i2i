using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class AssignmentConfigurationTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public AssignmentConfigurationTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    private static (Guid ideaId, Guid evaluatorId, Guid adminId, Guid pendingStatusId) SeedPrerequisites(InnovationDbContext context, string suffix)
    {
        var submitterId = Guid.NewGuid();
        var evaluatorId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var themeId = Guid.NewGuid();

        context.Users.Add(new User { Id = submitterId, SamAccountName = $"submitter-{suffix}", Email = $"sub-{suffix}@gac-demo.sa", FullNameAr = "أ", FullNameEn = "Submitter" });
        context.Users.Add(new User { Id = evaluatorId, SamAccountName = $"evaluator-{suffix}", Email = $"eval-{suffix}@gac-demo.sa", FullNameAr = "ب", FullNameEn = "Evaluator" });
        context.Users.Add(new User { Id = adminId, SamAccountName = $"admin-{suffix}", Email = $"admin-{suffix}@gac-demo.sa", FullNameAr = "ج", FullNameEn = "Admin" });
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

        var pendingStatusId = context.AssignmentStatuses.Single(s => s.Code == "pending").Id;
        return (ideaId, evaluatorId, adminId, pendingStatusId);
    }

    [Fact]
    public void SavesAssignmentWithRequiredRelationships()
    {
        Guid assignmentId;

        using (var context = _fixture.CreateContext())
        {
            var (ideaId, evaluatorId, adminId, pendingStatusId) = SeedPrerequisites(context, "asn-t3a");

            var assignment = new Assignment
            {
                Id = Guid.NewGuid(),
                IdeaId = ideaId,
                EvaluatorId = evaluatorId,
                AssignedById = adminId,
                AssignmentStatusId = pendingStatusId,
                DueAt = DateTime.UtcNow.AddDays(7),
            };
            assignmentId = assignment.Id;

            context.Assignments.Add(assignment);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var assignment = context.Assignments
                .Include(a => a.AssignmentStatus)
                .Single(a => a.Id == assignmentId);
            Assert.Equal("pending", assignment.AssignmentStatus.Code);
        }
    }

    [Fact]
    public void RejectsAssignmentStatusIdThatDoesNotExist()
    {
        using var context = _fixture.CreateContext();
        var (ideaId, evaluatorId, adminId, _) = SeedPrerequisites(context, "asn-t3b");

        context.Assignments.Add(new Assignment
        {
            Id = Guid.NewGuid(),
            IdeaId = ideaId,
            EvaluatorId = evaluatorId,
            AssignedById = adminId,
            AssignmentStatusId = Guid.NewGuid(),
        });

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }
}
