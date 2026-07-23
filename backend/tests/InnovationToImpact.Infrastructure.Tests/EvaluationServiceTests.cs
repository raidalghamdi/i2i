using InnovationToImpact.Domain.Audit;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Evaluations;
using InnovationToImpact.Domain.Ideas;
using InnovationToImpact.Domain.Notifications;
using InnovationToImpact.Infrastructure.Data;
using InnovationToImpact.Infrastructure.Evaluations;
using InnovationToImpact.Infrastructure.Ideas;
using InnovationToImpact.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class EvaluationServiceTests
{
    private sealed class FakeAuditLogWriter : IAuditLogWriter
    {
        public Task<AuditLog> AppendAsync(string entityType, Guid entityId, string action, Guid? actorId, string? payload, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AuditLog { Id = Guid.NewGuid(), EntityType = entityType, EntityId = entityId, Action = action, ActorId = actorId, Payload = payload });
        }
    }

    private sealed class FakeNotificationService : INotificationService
    {
        public Task<Notification> CreateAndPublishAsync(Guid userId, string notificationType, string titleAr, string titleEn, string bodyAr, string bodyEn, string? link, string? payloadJson, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new Notification { Id = Guid.NewGuid(), UserId = userId, NotificationType = notificationType, TitleAr = titleAr, TitleEn = titleEn, BodyAr = bodyAr, BodyEn = bodyEn });
        }
    }

    private static IdeaService MakeIdeaService(InnovationDbContext db, IEvidenceFileStorage storage) =>
        new(db, storage, new FakeAuditLogWriter(), new FakeNotificationService());

    private static Guid SeedUser(SqliteContextFixture fixture, string samAccountName)
    {
        using var db = fixture.CreateContext();
        var id = Guid.NewGuid();
        db.Users.Add(new User { Id = id, SamAccountName = samAccountName, Email = $"{samAccountName}@gac-demo.sa", FullNameAr = samAccountName, FullNameEn = samAccountName });
        db.SaveChanges();
        return id;
    }

    private static (LocalDiskFileStorage Storage, string RootPath) MakeStorage()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), $"evaluation-service-test-{Guid.NewGuid():N}");
        return (new LocalDiskFileStorage(rootPath), rootPath);
    }

    private static Guid SeedActivity(SqliteContextFixture fixture, Guid creatorId)
    {
        using var db = fixture.CreateContext();
        var id = Guid.NewGuid();
        db.Activities.Add(new Activity
        {
            Id = id,
            NameAr = "فعالية",
            NameEn = "Activity",
            Type = "hackathon",
            Status = "open",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            CreatedById = creatorId,
        });
        db.SaveChanges();
        return id;
    }

    private static async Task<Idea> CreateIdeaInEvaluationAsync(SqliteContextFixture fixture, Guid submitterId, Guid evaluatorId, Guid activityId, Guid themeId)
    {
        using var assignDb = fixture.CreateContext();
        assignDb.Set<EvaluatorTrackAssignment>().Add(new EvaluatorTrackAssignment { Id = Guid.NewGuid(), EvaluatorId = evaluatorId, TrackId = themeId, AssignedById = evaluatorId });
        assignDb.SaveChanges();

        var (storage, rootPath) = MakeStorage();
        try
        {
            using var db = fixture.CreateContext();
            var ideaService = MakeIdeaService(db, storage);
            var input = new IdeaInput("ا", "T", "م", "P", "ح", "S", "ف", "B", themeId, activityId, null, "individual", null, Array.Empty<TeamMemberInput>(), true, true);
            var created = await ideaService.CreateAsync(submitterId, input);
            await ideaService.AddAttachmentAsync(created.Idea!.Id, submitterId, "a.pdf", "application/pdf", new byte[] { 1 });
            await ideaService.SubmitAsync(created.Idea.Id, submitterId);

            // The supervisor slice's screening gate is what normally moves an idea from "submitted" to
            // "evaluation" now that IdeaService.SubmitAsync no longer auto-transitions. This helper only
            // needs an idea already in "evaluation" state, so it simulates "screening already approved
            // this idea" by setting the status directly (both FK and nav property, per this project's
            // stale-nav-property convention).
            var evaluationStatus = db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.Evaluation);
            var idea = db.Ideas.Single(i => i.Id == created.Idea.Id);
            idea.IdeaStatusId = evaluationStatus.Id;
            idea.IdeaStatus = evaluationStatus;
            db.SaveChanges();

            return idea;
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task SubmitAsync_ConfiguredThresholdAboveAverage_Fails()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedUser(fixture, "submitter1");
        var evaluatorId = SeedUser(fixture, "evaluator1");
        using var themeDb = fixture.CreateContext();
        var themeId = themeDb.StrategicThemes.First().Id;
        var idea = await CreateIdeaInEvaluationAsync(fixture, submitterId, evaluatorId, SeedActivity(fixture, submitterId), themeId);

        // Raise the passing threshold to 7.0.
        using (var setDb = fixture.CreateContext())
        {
            await new EvaluationSettingsService(setDb).UpdatePassThresholdAsync(7.0m, submitterId);
        }

        using var db = fixture.CreateContext();
        var service = new EvaluationService(db, new EvaluationSettingsService(db));
        // Average of these five = 6.4, which is >= 6.0 (would pass by default) but < 7.0 (configured).
        var input = new EvaluationInput(6, 7, 6, 7, 6, "Borderline.");

        var result = await service.SubmitAsync(idea.Id, evaluatorId, input);

        Assert.Equal(EvaluationCommandStatus.Success, result.Status);
        Assert.Equal(IdeaStatusCodes.EvaluationFailed, result.Idea!.IdeaStatus.Code);
    }

    [Fact]
    public async Task SubmitAsync_ScoreAtThreshold_TransitionsToPassAwaitingAttachments()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedUser(fixture, "submitter1");
        var evaluatorId = SeedUser(fixture, "evaluator1");
        using var themeDb = fixture.CreateContext();
        var themeId = themeDb.StrategicThemes.First().Id;
        var idea = await CreateIdeaInEvaluationAsync(fixture, submitterId, evaluatorId, SeedActivity(fixture, submitterId), themeId);

        using var db = fixture.CreateContext();
        var service = new EvaluationService(db, new EvaluationSettingsService(db));
        var input = new EvaluationInput(6, 6, 6, 6, 6, "Solid idea.");

        var result = await service.SubmitAsync(idea.Id, evaluatorId, input);

        Assert.Equal(EvaluationCommandStatus.Success, result.Status);
        Assert.Equal(6m, result.Evaluation!.TotalScore);
        Assert.Equal(EvaluationRecommendationCodes.Pass, result.Evaluation.Recommendation);
        Assert.Equal(IdeaStatusCodes.PassAwaitingAttachments, result.Idea!.IdeaStatus.Code);

        using var verifyDb = fixture.CreateContext();
        var reloaded = verifyDb.Ideas.Include(i => i.IdeaStatus).Single(i => i.Id == idea.Id);
        Assert.Equal(IdeaStatusCodes.PassAwaitingAttachments, reloaded.IdeaStatus.Code);
    }

    [Fact]
    public async Task SubmitAsync_ScoreBelowThreshold_TransitionsToEvaluationFailed()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedUser(fixture, "submitter1");
        var evaluatorId = SeedUser(fixture, "evaluator1");
        using var themeDb = fixture.CreateContext();
        var themeId = themeDb.StrategicThemes.First().Id;
        var idea = await CreateIdeaInEvaluationAsync(fixture, submitterId, evaluatorId, SeedActivity(fixture, submitterId), themeId);

        using var db = fixture.CreateContext();
        var service = new EvaluationService(db, new EvaluationSettingsService(db));
        var input = new EvaluationInput(5, 5, 6, 6, 5.5m, null);

        var result = await service.SubmitAsync(idea.Id, evaluatorId, input);

        Assert.Equal(EvaluationCommandStatus.Success, result.Status);
        Assert.Equal(5.5m, result.Evaluation!.TotalScore);
        Assert.Equal(EvaluationRecommendationCodes.Fail, result.Evaluation.Recommendation);
        Assert.Equal(IdeaStatusCodes.EvaluationFailed, result.Idea!.IdeaStatus.Code);
    }

    [Fact]
    public async Task SubmitAsync_NotAssignedToTrack_ReturnsForbidden()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedUser(fixture, "submitter1");
        var evaluatorId = SeedUser(fixture, "evaluator1");
        var otherEvaluatorId = SeedUser(fixture, "evaluator2");
        using var themeDb = fixture.CreateContext();
        var themeId = themeDb.StrategicThemes.First().Id;
        var idea = await CreateIdeaInEvaluationAsync(fixture, submitterId, evaluatorId, SeedActivity(fixture, submitterId), themeId);

        using var db = fixture.CreateContext();
        var service = new EvaluationService(db, new EvaluationSettingsService(db));
        var input = new EvaluationInput(8, 8, 8, 8, 8, null);

        var result = await service.SubmitAsync(idea.Id, otherEvaluatorId, input);

        Assert.Equal(EvaluationCommandStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task SubmitAsync_IdeaNotInEvaluationState_ReturnsInvalidState()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedUser(fixture, "submitter1");
        var evaluatorId = SeedUser(fixture, "evaluator1");
        using var themeDb = fixture.CreateContext();
        var themeId = themeDb.StrategicThemes.First().Id;

        var activityId = SeedActivity(fixture, submitterId);
        var (storage, rootPath) = MakeStorage();
        try
        {
            using var db = fixture.CreateContext();
            var ideaService = MakeIdeaService(db, storage);
            var input = new IdeaInput("ا", "T", "م", "P", "ح", "S", "ف", "B", themeId, activityId, null, "individual", null, Array.Empty<TeamMemberInput>(), true, true);
            var created = await ideaService.CreateAsync(submitterId, input);

            db.Set<EvaluatorTrackAssignment>().Add(new EvaluatorTrackAssignment { Id = Guid.NewGuid(), EvaluatorId = evaluatorId, TrackId = themeId, AssignedById = evaluatorId });
            db.SaveChanges();

            var service = new EvaluationService(db, new EvaluationSettingsService(db));
            var evalInput = new EvaluationInput(8, 8, 8, 8, 8, null);

            var result = await service.SubmitAsync(created.Idea!.Id, evaluatorId, evalInput);

            Assert.Equal(EvaluationCommandStatus.InvalidState, result.Status);
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task SubmitAsync_AlreadyEvaluatedByThisEvaluator_ReturnsAlreadyEvaluated()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedUser(fixture, "submitter1");
        var evaluatorId = SeedUser(fixture, "evaluator1");
        using var themeDb = fixture.CreateContext();
        var themeId = themeDb.StrategicThemes.First().Id;
        var idea = await CreateIdeaInEvaluationAsync(fixture, submitterId, evaluatorId, SeedActivity(fixture, submitterId), themeId);

        using var db = fixture.CreateContext();
        var service = new EvaluationService(db, new EvaluationSettingsService(db));
        var input = new EvaluationInput(8, 8, 8, 8, 8, null);
        await service.SubmitAsync(idea.Id, evaluatorId, input);

        // The idea's status has now moved away from "evaluation" (to pass_awaiting_attachments).
        // To isolate the AlreadyEvaluated check itself (rather than re-hitting InvalidState),
        // manually move the idea's status back to "evaluation" before retrying with the same evaluator.
        using var reopenDb = fixture.CreateContext();
        var evaluationStatus = reopenDb.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.Evaluation);
        var reopenedIdea = reopenDb.Ideas.Single(i => i.Id == idea.Id);
        reopenedIdea.IdeaStatusId = evaluationStatus.Id;
        reopenDb.SaveChanges();

        using var retryDb = fixture.CreateContext();
        var retryService = new EvaluationService(retryDb, new EvaluationSettingsService(retryDb));
        var retryResult = await retryService.SubmitAsync(idea.Id, evaluatorId, input);

        Assert.Equal(EvaluationCommandStatus.AlreadyEvaluated, retryResult.Status);
    }

    [Fact]
    public async Task SubmitAsync_ScoreOutOfRange_ReturnsInvalidScore()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedUser(fixture, "submitter1");
        var evaluatorId = SeedUser(fixture, "evaluator1");
        using var themeDb = fixture.CreateContext();
        var themeId = themeDb.StrategicThemes.First().Id;
        var idea = await CreateIdeaInEvaluationAsync(fixture, submitterId, evaluatorId, SeedActivity(fixture, submitterId), themeId);

        using var db = fixture.CreateContext();
        var service = new EvaluationService(db, new EvaluationSettingsService(db));
        var input = new EvaluationInput(11, 8, 8, 8, 8, null);

        var result = await service.SubmitAsync(idea.Id, evaluatorId, input);

        Assert.Equal(EvaluationCommandStatus.InvalidScore, result.Status);
    }

    [Fact]
    public async Task GetQueueAsync_ReturnsOnlyIdeasInAssignedTracksAndEvaluationStatus()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedUser(fixture, "submitter1");
        var evaluatorId = SeedUser(fixture, "evaluator1");
        using var themeDb = fixture.CreateContext();
        var themes = themeDb.StrategicThemes.ToList();
        var assignedThemeId = themes[0].Id;
        var unassignedThemeId = themes[1].Id;

        var inQueueIdea = await CreateIdeaInEvaluationAsync(fixture, submitterId, evaluatorId, SeedActivity(fixture, submitterId), assignedThemeId);

        var (storage, rootPath) = MakeStorage();
        try
        {
            using var otherDb = fixture.CreateContext();
            var ideaService = MakeIdeaService(otherDb, storage);
            var otherActivityId = SeedActivity(fixture, submitterId);
            var otherInput = new IdeaInput("ا", "Other", "م", "P", "ح", "S", "ف", "B", unassignedThemeId, otherActivityId, null, "individual", null, Array.Empty<TeamMemberInput>(), true, true);
            var otherCreated = await ideaService.CreateAsync(submitterId, otherInput);
            await ideaService.AddAttachmentAsync(otherCreated.Idea!.Id, submitterId, "a.pdf", "application/pdf", new byte[] { 1 });
            await ideaService.SubmitAsync(otherCreated.Idea.Id, submitterId);

            using var db = fixture.CreateContext();
            var service = new EvaluationService(db, new EvaluationSettingsService(db));
            var queue = await service.GetQueueAsync(evaluatorId);

            Assert.Single(queue);
            Assert.Equal(inQueueIdea.Id, queue[0].Id);
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task GetMyEvaluationsAsync_ReturnsOnlyCallersEvaluations()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedUser(fixture, "submitter1");
        var evaluatorId = SeedUser(fixture, "evaluator1");
        var otherEvaluatorId = SeedUser(fixture, "evaluator2");
        using var themeDb = fixture.CreateContext();
        var themes = themeDb.StrategicThemes.ToList();
        var idea1 = await CreateIdeaInEvaluationAsync(fixture, submitterId, evaluatorId, SeedActivity(fixture, submitterId), themes[0].Id);
        var idea2 = await CreateIdeaInEvaluationAsync(fixture, submitterId, otherEvaluatorId, SeedActivity(fixture, submitterId), themes[1].Id);

        using var db = fixture.CreateContext();
        var service = new EvaluationService(db, new EvaluationSettingsService(db));
        await service.SubmitAsync(idea1.Id, evaluatorId, new EvaluationInput(8, 8, 8, 8, 8, null));
        await service.SubmitAsync(idea2.Id, otherEvaluatorId, new EvaluationInput(7, 7, 7, 7, 7, null));

        var mine = await service.GetMyEvaluationsAsync(evaluatorId);

        Assert.Single(mine);
        Assert.Equal(idea1.Id, mine[0].IdeaId);
    }
}
