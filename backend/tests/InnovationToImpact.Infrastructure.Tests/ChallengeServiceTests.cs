using InnovationToImpact.Domain.Audit;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Ideas;
using InnovationToImpact.Infrastructure.Ideas;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class ChallengeServiceTests
{
    private sealed class FakeAuditLogWriter : IAuditLogWriter
    {
        public Task<AuditLog> AppendAsync(string entityType, Guid entityId, string action, Guid? actorId, string? payload, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AuditLog { Id = Guid.NewGuid(), EntityType = entityType, EntityId = entityId, Action = action, ActorId = actorId, Payload = payload });
    }

    [Fact]
    public async Task CreateAsync_ValidInput_PersistsChallenge()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;
        var actorId = Guid.NewGuid();
        var service = new ChallengeService(db, new FakeAuditLogWriter());

        var result = await service.CreateAsync(new ChallengeInput(themeId, "تحدٍ", "Challenge", 0, true), actorId);

        Assert.Equal(ChallengeCommandStatus.Success, result.Status);
        Assert.Equal("Challenge", result.Entity!.TextEn);
    }

    [Fact]
    public async Task CreateAsync_ThemeDoesNotExist_ReturnsInvalidStrategicTheme()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var service = new ChallengeService(db, new FakeAuditLogWriter());

        var result = await service.CreateAsync(new ChallengeInput(Guid.NewGuid(), "تحدٍ", "Challenge", 0, true), Guid.NewGuid());

        Assert.Equal(ChallengeCommandStatus.InvalidStrategicTheme, result.Status);
    }

    [Fact]
    public async Task ListActiveByThemeAsync_ExcludesInactiveAndOtherThemes()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var themes = db.StrategicThemes.ToList();
        var themeId = themes[0].Id;
        var otherThemeId = themes[1].Id;
        db.Challenges.AddRange(
            new Challenge { Id = Guid.NewGuid(), StrategicThemeId = themeId, TextAr = "أ", TextEn = "Active", SortOrder = 0, IsActive = true },
            new Challenge { Id = Guid.NewGuid(), StrategicThemeId = themeId, TextAr = "ب", TextEn = "Inactive", SortOrder = 1, IsActive = false },
            new Challenge { Id = Guid.NewGuid(), StrategicThemeId = otherThemeId, TextAr = "ج", TextEn = "OtherTheme", SortOrder = 0, IsActive = true });
        db.SaveChanges();
        var service = new ChallengeService(db, new FakeAuditLogWriter());

        var result = await service.ListActiveByThemeAsync(themeId);

        Assert.Single(result);
        Assert.Equal("Active", result[0].TextEn);
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ReturnsNotFound()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;
        var service = new ChallengeService(db, new FakeAuditLogWriter());

        var result = await service.UpdateAsync(Guid.NewGuid(), new ChallengeInput(themeId, "أ", "A", 0, true), Guid.NewGuid());

        Assert.Equal(ChallengeCommandStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task DeleteAsync_RemovesChallenge()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;
        var challenge = new Challenge { Id = Guid.NewGuid(), StrategicThemeId = themeId, TextAr = "أ", TextEn = "A", SortOrder = 0, IsActive = true };
        db.Challenges.Add(challenge);
        db.SaveChanges();
        var service = new ChallengeService(db, new FakeAuditLogWriter());

        var result = await service.DeleteAsync(challenge.Id, Guid.NewGuid());

        Assert.Equal(ChallengeCommandStatus.Success, result.Status);
        Assert.False(db.Challenges.Any(c => c.Id == challenge.Id));
    }

    [Fact]
    public async Task DeleteAsync_ChallengeReferencedByAnIdea_ReturnsInUse()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;
        var challenge = new Challenge { Id = Guid.NewGuid(), StrategicThemeId = themeId, TextAr = "أ", TextEn = "A", SortOrder = 0, IsActive = true };
        db.Challenges.Add(challenge);

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
            ChallengeId = challenge.Id,
            IdeaStatusId = draftStatus.Id,
            SubmitterId = submitterId,
        });
        db.SaveChanges();

        var service = new ChallengeService(db, new FakeAuditLogWriter());
        var result = await service.DeleteAsync(challenge.Id, Guid.NewGuid());

        Assert.Equal(ChallengeCommandStatus.InUse, result.Status);
        using var readDb = fixture.CreateContext();
        Assert.True(readDb.Challenges.Any(c => c.Id == challenge.Id));
    }
}
