using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class EvaluatorTrackAssignmentConfigurationTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public EvaluatorTrackAssignmentConfigurationTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    private static (Guid evaluatorId, Guid trackId, Guid adminId) SeedPrerequisites(InnovationDbContext context, string suffix)
    {
        var evaluatorId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var trackId = Guid.NewGuid();

        context.Users.Add(new User { Id = evaluatorId, SamAccountName = $"evaluator-{suffix}", Email = $"eval-{suffix}@gac-demo.sa", FullNameAr = "أ", FullNameEn = "Evaluator" });
        context.Users.Add(new User { Id = adminId, SamAccountName = $"admin-{suffix}", Email = $"admin-{suffix}@gac-demo.sa", FullNameAr = "ب", FullNameEn = "Admin" });
        context.StrategicThemes.Add(new StrategicTheme { Id = trackId, NameAr = "محور", NameEn = "Track", OwnerId = adminId });
        context.SaveChanges();

        return (evaluatorId, trackId, adminId);
    }

    [Fact]
    public void SavesEvaluatorTrackAssignment()
    {
        Guid assignmentId;

        using (var context = _fixture.CreateContext())
        {
            var (evaluatorId, trackId, adminId) = SeedPrerequisites(context, "eta-t4a");

            var assignment = new EvaluatorTrackAssignment
            {
                Id = Guid.NewGuid(),
                EvaluatorId = evaluatorId,
                TrackId = trackId,
                AssignedById = adminId,
            };
            assignmentId = assignment.Id;

            context.EvaluatorTrackAssignments.Add(assignment);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var assignment = context.EvaluatorTrackAssignments.Single(a => a.Id == assignmentId);
            Assert.NotEqual(Guid.Empty, assignment.EvaluatorId);
        }
    }

    [Fact]
    public void RejectsDuplicateEvaluatorTrackPair()
    {
        using var context = _fixture.CreateContext();
        var (evaluatorId, trackId, adminId) = SeedPrerequisites(context, "eta-t4b");

        context.EvaluatorTrackAssignments.Add(new EvaluatorTrackAssignment { Id = Guid.NewGuid(), EvaluatorId = evaluatorId, TrackId = trackId, AssignedById = adminId });
        context.SaveChanges();

        context.EvaluatorTrackAssignments.Add(new EvaluatorTrackAssignment { Id = Guid.NewGuid(), EvaluatorId = evaluatorId, TrackId = trackId, AssignedById = adminId });

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }
}
