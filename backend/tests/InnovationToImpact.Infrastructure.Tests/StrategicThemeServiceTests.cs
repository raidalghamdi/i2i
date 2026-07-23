using InnovationToImpact.Domain.Audit;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.StrategicThemes;
using InnovationToImpact.Infrastructure.StrategicThemes;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class StrategicThemeServiceTests
{
    private sealed class FakeAuditLogWriter : IAuditLogWriter
    {
        public Task<AuditLog> AppendAsync(string entityType, Guid entityId, string action, Guid? actorId, string? payload, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AuditLog { Id = Guid.NewGuid(), EntityType = entityType, EntityId = entityId, Action = action, ActorId = actorId, Payload = payload });
    }

    [Fact]
    public async Task CreateAsync_ValidInput_SetsPriorityToMaxPlusOneAndOwnerToActor()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var actorId = Guid.NewGuid();
        db.Users.Add(new User { Id = actorId, SamAccountName = "actor1", Email = "actor1@gac-demo.sa", FullNameAr = "a1", FullNameEn = "a1" });
        db.SaveChanges();
        var service = new StrategicThemeService(db, new FakeAuditLogWriter());
        var maxExistingPriority = db.StrategicThemes.Max(t => t.Priority);

        var result = await service.CreateAsync(new StrategicThemeInput("مسار جديد", "New Track", "وصف", "Description"), actorId);

        Assert.Equal(StrategicThemeCommandStatus.Success, result.Status);
        Assert.Equal("New Track", result.Entity!.NameEn);
        Assert.Equal(maxExistingPriority + 1, result.Entity.Priority);
        Assert.Equal(actorId, result.Entity.OwnerId);
    }

    [Fact]
    public async Task CreateAsync_MissingNameEn_ReturnsInvalidInput()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var service = new StrategicThemeService(db, new FakeAuditLogWriter());

        var result = await service.CreateAsync(new StrategicThemeInput("مسار", "", null, null), Guid.NewGuid());

        Assert.Equal(StrategicThemeCommandStatus.InvalidInput, result.Status);
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ReturnsNotFound()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var service = new StrategicThemeService(db, new FakeAuditLogWriter());

        var result = await service.UpdateAsync(Guid.NewGuid(), new StrategicThemeInput("أ", "A", null, null), Guid.NewGuid());

        Assert.Equal(StrategicThemeCommandStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheme()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var service = new StrategicThemeService(db, new FakeAuditLogWriter());
        var themeId = Guid.NewGuid();
        db.StrategicThemes.Add(new StrategicTheme { Id = themeId, NameAr = "أ", NameEn = "A", Priority = 99, OwnerId = db.Users.Any() ? db.Users.First().Id : Guid.NewGuid() });
        db.SaveChanges();

        var result = await service.DeleteAsync(themeId, Guid.NewGuid());

        Assert.Equal(StrategicThemeCommandStatus.Success, result.Status);
        Assert.False(db.StrategicThemes.Any(t => t.Id == themeId));
    }

    [Fact]
    public async Task DeleteAsync_ThemeReferencedByAnIdea_ReturnsInUse()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;

        var submitterId = Guid.NewGuid();
        db.Users.Add(new User { Id = submitterId, SamAccountName = "submitter1", Email = "submitter1@gac-demo.sa", FullNameAr = "s1", FullNameEn = "s1" });
        var activityId = Guid.NewGuid();
        db.Activities.Add(new Activity { Id = activityId, NameAr = "ف", NameEn = "Activity", Type = "hackathon", Status = "open", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(30), CreatedById = submitterId });
        var draftStatus = db.IdeaStatuses.Single(s => s.Code == "draft");
        db.Ideas.Add(new Idea
        {
            Id = Guid.NewGuid(),
            Code = "IDEA-0001",
            TitleAr = "ا", TitleEn = "T",
            ProblemStatementAr = "م", ProblemStatementEn = "P",
            ProposedSolutionAr = "ح", ProposedSolutionEn = "S",
            ExpectedBenefitsAr = "ف", ExpectedBenefitsEn = "B",
            StrategicThemeId = themeId,
            ActivityId = activityId,
            IdeaStatusId = draftStatus.Id,
            SubmitterId = submitterId,
        });
        db.SaveChanges();

        var service = new StrategicThemeService(db, new FakeAuditLogWriter());
        var result = await service.DeleteAsync(themeId, Guid.NewGuid());

        Assert.Equal(StrategicThemeCommandStatus.InUse, result.Status);
        using var readDb = fixture.CreateContext();
        Assert.True(readDb.StrategicThemes.Any(t => t.Id == themeId));
    }
}
