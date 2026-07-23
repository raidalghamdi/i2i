using InnovationToImpact.Domain.Auth;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.TrackAssignments;
using InnovationToImpact.Infrastructure.TrackAssignments;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class TrackAssignmentServiceTests
{
    private static Guid SeedUserWithRole(SqliteContextFixture fixture, string samAccountName, string? roleCode)
    {
        using var db = fixture.CreateContext();
        var id = Guid.NewGuid();
        db.Users.Add(new User { Id = id, SamAccountName = samAccountName, Email = $"{samAccountName}@gac-demo.sa", FullNameAr = samAccountName, FullNameEn = samAccountName });
        db.SaveChanges();

        if (roleCode is not null)
        {
            var roleId = db.Roles.Single(r => r.Code == roleCode).Id;
            db.Set<UserRole>().Add(new UserRole { UserId = id, RoleId = roleId, IsPrimary = true });
            db.SaveChanges();
        }

        return id;
    }

    [Fact]
    public async Task AssignAsync_EvaluatorRoleUser_Succeeds()
    {
        using var fixture = new SqliteContextFixture();
        var evaluatorId = SeedUserWithRole(fixture, "evaluator1", RoleCodes.Evaluator);
        var supervisorId = SeedUserWithRole(fixture, "supervisor1", RoleCodes.Supervisor);
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;
        var service = new TrackAssignmentService(db);

        var result = await service.AssignAsync(evaluatorId, themeId, supervisorId);

        Assert.Equal(TrackAssignmentCommandStatus.Success, result.Status);
        Assert.Equal(evaluatorId, result.Assignment!.EvaluatorId);
        Assert.Equal(themeId, result.Assignment.TrackId);
    }

    [Fact]
    public async Task AssignAsync_NonEvaluatorRoleUser_ReturnsInvalidEvaluator()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedUserWithRole(fixture, "submitter1", RoleCodes.Submitter);
        var supervisorId = SeedUserWithRole(fixture, "supervisor1", RoleCodes.Supervisor);
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;
        var service = new TrackAssignmentService(db);

        var result = await service.AssignAsync(submitterId, themeId, supervisorId);

        Assert.Equal(TrackAssignmentCommandStatus.InvalidEvaluator, result.Status);
    }

    [Fact]
    public async Task AssignAsync_Duplicate_ReturnsAlreadyAssigned()
    {
        using var fixture = new SqliteContextFixture();
        var evaluatorId = SeedUserWithRole(fixture, "evaluator1", RoleCodes.Evaluator);
        var supervisorId = SeedUserWithRole(fixture, "supervisor1", RoleCodes.Supervisor);
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;
        var service = new TrackAssignmentService(db);
        await service.AssignAsync(evaluatorId, themeId, supervisorId);

        var result = await service.AssignAsync(evaluatorId, themeId, supervisorId);

        Assert.Equal(TrackAssignmentCommandStatus.AlreadyAssigned, result.Status);
    }

    [Fact]
    public async Task RemoveAsync_ExistingAssignment_Succeeds()
    {
        using var fixture = new SqliteContextFixture();
        var evaluatorId = SeedUserWithRole(fixture, "evaluator1", RoleCodes.Evaluator);
        var supervisorId = SeedUserWithRole(fixture, "supervisor1", RoleCodes.Supervisor);
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;
        var service = new TrackAssignmentService(db);
        var created = await service.AssignAsync(evaluatorId, themeId, supervisorId);

        var result = await service.RemoveAsync(created.Assignment!.Id);

        Assert.Equal(TrackAssignmentCommandStatus.Success, result.Status);
        Assert.Empty(await service.ListAsync());
    }

    [Fact]
    public async Task RemoveAsync_NotFound_ReturnsNotFound()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var service = new TrackAssignmentService(db);

        var result = await service.RemoveAsync(Guid.NewGuid());

        Assert.Equal(TrackAssignmentCommandStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task ListAsync_ReturnsAllAssignmentsWithNavigationPropertiesLoaded()
    {
        using var fixture = new SqliteContextFixture();
        var evaluatorId = SeedUserWithRole(fixture, "evaluator1", RoleCodes.Evaluator);
        var supervisorId = SeedUserWithRole(fixture, "supervisor1", RoleCodes.Supervisor);
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;
        var service = new TrackAssignmentService(db);
        await service.AssignAsync(evaluatorId, themeId, supervisorId);

        var list = await service.ListAsync();

        Assert.Single(list);
        Assert.Equal("evaluator1", list[0].Evaluator.FullNameEn);
        Assert.NotNull(list[0].Track.NameEn);
    }
}
