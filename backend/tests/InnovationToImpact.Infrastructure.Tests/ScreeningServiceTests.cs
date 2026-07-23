using InnovationToImpact.Domain.Audit;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Ideas;
using InnovationToImpact.Domain.Notifications;
using InnovationToImpact.Domain.Screening;
using InnovationToImpact.Infrastructure.Data;
using InnovationToImpact.Infrastructure.Ideas;
using InnovationToImpact.Infrastructure.Screening;
using InnovationToImpact.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class ScreeningServiceTests
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

    private static async Task<Idea> CreateSubmittedIdeaAsync(SqliteContextFixture fixture, Guid submitterId, Guid activityId, Guid themeId)
    {
        var rootPath = Path.Combine(Path.GetTempPath(), $"screening-service-test-{Guid.NewGuid():N}");
        var storage = new LocalDiskFileStorage(rootPath);
        try
        {
            using var db = fixture.CreateContext();
            var ideaService = MakeIdeaService(db, storage);
            var input = new IdeaInput("ا", "T", "م", "P", "ح", "S", "ف", "B", themeId, activityId, null, "individual", null, Array.Empty<TeamMemberInput>(), true, true);
            var created = await ideaService.CreateAsync(submitterId, input);
            await ideaService.AddAttachmentAsync(created.Idea!.Id, submitterId, "a.pdf", "application/pdf", new byte[] { 1 });
            var submitted = await ideaService.SubmitAsync(created.Idea.Id, submitterId);
            return submitted.Idea!;
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task SubmitDecisionAsync_Approve_TransitionsToEvaluation()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedUser(fixture, "submitter1");
        var supervisorId = SeedUser(fixture, "supervisor1");
        using var themeDb = fixture.CreateContext();
        var themeId = themeDb.StrategicThemes.First().Id;
        var idea = await CreateSubmittedIdeaAsync(fixture, submitterId, SeedActivity(fixture, submitterId), themeId);

        using var db = fixture.CreateContext();
        var service = new ScreeningService(db);

        var result = await service.SubmitDecisionAsync(idea.Id, supervisorId, new ScreeningDecisionInput("approve", null));

        Assert.Equal(ScreeningCommandStatus.Success, result.Status);
        Assert.Equal(IdeaStatusCodes.Evaluation, result.Idea!.IdeaStatus.Code);
    }

    [Fact]
    public async Task SubmitDecisionAsync_Approve_SetsEnteredEvaluationAt()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedUser(fixture, "submitter1");
        var supervisorId = SeedUser(fixture, "supervisor1");
        using var themeDb = fixture.CreateContext();
        var themeId = themeDb.StrategicThemes.First().Id;
        var idea = await CreateSubmittedIdeaAsync(fixture, submitterId, SeedActivity(fixture, submitterId), themeId);

        using var db = fixture.CreateContext();
        var service = new ScreeningService(db);

        var before = DateTime.UtcNow;
        var result = await service.SubmitDecisionAsync(idea.Id, supervisorId, new ScreeningDecisionInput("approve", null));
        var after = DateTime.UtcNow;

        Assert.NotNull(result.Idea!.EnteredEvaluationAt);
        Assert.InRange(result.Idea.EnteredEvaluationAt!.Value, before, after);
    }

    [Fact]
    public async Task SubmitDecisionAsync_Reject_DoesNotSetEnteredEvaluationAt()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedUser(fixture, "submitter1");
        var supervisorId = SeedUser(fixture, "supervisor1");
        using var themeDb = fixture.CreateContext();
        var themeId = themeDb.StrategicThemes.First().Id;
        var idea = await CreateSubmittedIdeaAsync(fixture, submitterId, SeedActivity(fixture, submitterId), themeId);

        using var db = fixture.CreateContext();
        var service = new ScreeningService(db);

        var result = await service.SubmitDecisionAsync(idea.Id, supervisorId, new ScreeningDecisionInput("reject", "Not aligned with program goals."));

        Assert.Null(result.Idea!.EnteredEvaluationAt);
    }

    [Fact]
    public async Task SubmitDecisionAsync_Reject_WithReason_TransitionsToRejectedAndStoresReason()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedUser(fixture, "submitter1");
        var supervisorId = SeedUser(fixture, "supervisor1");
        using var themeDb = fixture.CreateContext();
        var themeId = themeDb.StrategicThemes.First().Id;
        var idea = await CreateSubmittedIdeaAsync(fixture, submitterId, SeedActivity(fixture, submitterId), themeId);

        using var db = fixture.CreateContext();
        var service = new ScreeningService(db);

        var result = await service.SubmitDecisionAsync(idea.Id, supervisorId, new ScreeningDecisionInput("reject", "Duplicate of an existing initiative."));

        Assert.Equal(ScreeningCommandStatus.Success, result.Status);
        Assert.Equal(IdeaStatusCodes.Rejected, result.Idea!.IdeaStatus.Code);
        Assert.Equal("Duplicate of an existing initiative.", result.Idea.ScreeningReason);
    }

    [Fact]
    public async Task SubmitDecisionAsync_Reject_WithoutReason_ReturnsReasonRequired()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedUser(fixture, "submitter1");
        var supervisorId = SeedUser(fixture, "supervisor1");
        using var themeDb = fixture.CreateContext();
        var themeId = themeDb.StrategicThemes.First().Id;
        var idea = await CreateSubmittedIdeaAsync(fixture, submitterId, SeedActivity(fixture, submitterId), themeId);

        using var db = fixture.CreateContext();
        var service = new ScreeningService(db);

        var result = await service.SubmitDecisionAsync(idea.Id, supervisorId, new ScreeningDecisionInput("reject", ""));

        Assert.Equal(ScreeningCommandStatus.ReasonRequired, result.Status);
    }

    [Fact]
    public async Task SubmitDecisionAsync_Return_WithShortReason_ReturnsReasonRequired()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedUser(fixture, "submitter1");
        var supervisorId = SeedUser(fixture, "supervisor1");
        using var themeDb = fixture.CreateContext();
        var themeId = themeDb.StrategicThemes.First().Id;
        var idea = await CreateSubmittedIdeaAsync(fixture, submitterId, SeedActivity(fixture, submitterId), themeId);

        using var db = fixture.CreateContext();
        var service = new ScreeningService(db);

        var result = await service.SubmitDecisionAsync(idea.Id, supervisorId, new ScreeningDecisionInput("return", "too short"));

        Assert.Equal(ScreeningCommandStatus.ReasonRequired, result.Status);
    }

    [Fact]
    public async Task SubmitDecisionAsync_Return_WithValidReason_TransitionsToReturned()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedUser(fixture, "submitter1");
        var supervisorId = SeedUser(fixture, "supervisor1");
        using var themeDb = fixture.CreateContext();
        var themeId = themeDb.StrategicThemes.First().Id;
        var idea = await CreateSubmittedIdeaAsync(fixture, submitterId, SeedActivity(fixture, submitterId), themeId);

        using var db = fixture.CreateContext();
        var service = new ScreeningService(db);

        var result = await service.SubmitDecisionAsync(idea.Id, supervisorId, new ScreeningDecisionInput("return", "Please clarify the budget impact section."));

        Assert.Equal(ScreeningCommandStatus.Success, result.Status);
        Assert.Equal(IdeaStatusCodes.Returned, result.Idea!.IdeaStatus.Code);
        Assert.Equal("Please clarify the budget impact section.", result.Idea.ScreeningReason);
    }

    [Fact]
    public async Task SubmitDecisionAsync_InvalidDecisionCode_ReturnsInvalidDecision()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedUser(fixture, "submitter1");
        var supervisorId = SeedUser(fixture, "supervisor1");
        using var themeDb = fixture.CreateContext();
        var themeId = themeDb.StrategicThemes.First().Id;
        var idea = await CreateSubmittedIdeaAsync(fixture, submitterId, SeedActivity(fixture, submitterId), themeId);

        using var db = fixture.CreateContext();
        var service = new ScreeningService(db);

        var result = await service.SubmitDecisionAsync(idea.Id, supervisorId, new ScreeningDecisionInput("maybe", null));

        Assert.Equal(ScreeningCommandStatus.InvalidDecision, result.Status);
    }

    [Fact]
    public async Task SubmitDecisionAsync_IdeaNotSubmitted_ReturnsInvalidState()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedUser(fixture, "submitter1");
        var supervisorId = SeedUser(fixture, "supervisor1");
        using var themeDb = fixture.CreateContext();
        var themeId = themeDb.StrategicThemes.First().Id;

        var rootPath = Path.Combine(Path.GetTempPath(), $"screening-service-test-{Guid.NewGuid():N}");
        var storage = new LocalDiskFileStorage(rootPath);
        Idea draftIdea;
        try
        {
            using var db = fixture.CreateContext();
            var ideaService = MakeIdeaService(db, storage);
            var activityId = SeedActivity(fixture, submitterId);
            var created = await ideaService.CreateAsync(submitterId, new IdeaInput("ا", "T", "م", "P", "ح", "S", "ف", "B", themeId, activityId, null, "individual", null, Array.Empty<TeamMemberInput>(), true, true));
            draftIdea = created.Idea!;
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }

        using var serviceDb = fixture.CreateContext();
        var service = new ScreeningService(serviceDb);

        var result = await service.SubmitDecisionAsync(draftIdea.Id, supervisorId, new ScreeningDecisionInput("approve", null));

        Assert.Equal(ScreeningCommandStatus.InvalidState, result.Status);
    }

    [Fact]
    public async Task SubmitDecisionAsync_NotFound_ReturnsNotFound()
    {
        using var fixture = new SqliteContextFixture();
        var supervisorId = SeedUser(fixture, "supervisor1");
        using var db = fixture.CreateContext();
        var service = new ScreeningService(db);

        var result = await service.SubmitDecisionAsync(Guid.NewGuid(), supervisorId, new ScreeningDecisionInput("approve", null));

        Assert.Equal(ScreeningCommandStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task GetQueueAsync_ReturnsOnlySubmittedIdeas()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedUser(fixture, "submitter1");
        var supervisorId = SeedUser(fixture, "supervisor1");
        using var themeDb = fixture.CreateContext();
        var themeId = themeDb.StrategicThemes.First().Id;
        var submittedIdea = await CreateSubmittedIdeaAsync(fixture, submitterId, SeedActivity(fixture, submitterId), themeId);
        var evaluationIdea = await CreateSubmittedIdeaAsync(fixture, submitterId, SeedActivity(fixture, submitterId), themeId);

        using (var approveDb = fixture.CreateContext())
        {
            var service = new ScreeningService(approveDb);
            await service.SubmitDecisionAsync(evaluationIdea.Id, supervisorId, new ScreeningDecisionInput("approve", null));
        }

        using var db = fixture.CreateContext();
        var queueService = new ScreeningService(db);

        var queue = await queueService.GetQueueAsync();

        Assert.Single(queue);
        Assert.Equal(submittedIdea.Id, queue[0].Id);
    }

    [Fact]
    public async Task SubmitDecisionAsync_Return_StoresEditableSections()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedUser(fixture, "submitter1");
        var supervisorId = SeedUser(fixture, "supervisor1");
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;
        var idea = await CreateSubmittedIdeaAsync(fixture, submitterId, SeedActivity(fixture, submitterId), themeId);

        using var serviceDb = fixture.CreateContext();
        var service = new ScreeningService(serviceDb);
        var input = new ScreeningDecisionInput("return", "Please add more detail to the proposal.", new[] { "title", "proposed_solution" });
        var result = await service.SubmitDecisionAsync(idea.Id, supervisorId, input);

        Assert.Equal(ScreeningCommandStatus.Success, result.Status);
        Assert.Equal("title,proposed_solution", result.Idea!.EditableSections);
    }

    [Fact]
    public async Task SubmitDecisionAsync_ReturnWithUnknownSectionKey_ReturnsInvalidDecision()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedUser(fixture, "submitter1");
        var supervisorId = SeedUser(fixture, "supervisor1");
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;
        var idea = await CreateSubmittedIdeaAsync(fixture, submitterId, SeedActivity(fixture, submitterId), themeId);

        using var serviceDb = fixture.CreateContext();
        var service = new ScreeningService(serviceDb);
        var input = new ScreeningDecisionInput("return", "Please add more detail to the proposal.", new[] { "not_a_real_section" });
        var result = await service.SubmitDecisionAsync(idea.Id, supervisorId, input);

        Assert.Equal(ScreeningCommandStatus.InvalidDecision, result.Status);
    }

    [Fact]
    public async Task SubmitDecisionAsync_ReturnWithNoSections_LeavesEditableSectionsNull()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedUser(fixture, "submitter1");
        var supervisorId = SeedUser(fixture, "supervisor1");
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;
        var idea = await CreateSubmittedIdeaAsync(fixture, submitterId, SeedActivity(fixture, submitterId), themeId);

        using var serviceDb = fixture.CreateContext();
        var service = new ScreeningService(serviceDb);
        var input = new ScreeningDecisionInput("return", "Please add more detail to the proposal.");
        var result = await service.SubmitDecisionAsync(idea.Id, supervisorId, input);

        Assert.Equal(ScreeningCommandStatus.Success, result.Status);
        Assert.Null(result.Idea!.EditableSections);
    }
}
