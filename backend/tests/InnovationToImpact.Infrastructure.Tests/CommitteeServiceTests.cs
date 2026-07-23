using InnovationToImpact.Domain.Audit;
using InnovationToImpact.Domain.Auth;
using InnovationToImpact.Domain.Committee;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Ideas;
using InnovationToImpact.Domain.Notifications;
using InnovationToImpact.Infrastructure.Approvals;
using InnovationToImpact.Infrastructure.Committee;
using InnovationToImpact.Infrastructure.Data;
using InnovationToImpact.Infrastructure.Ideas;
using InnovationToImpact.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class CommitteeServiceTests
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

    // Real ApprovalService (backed by the same fixture db) rather than a no-op fake, so the new
    // gate-wiring test below can assert a committee-publish ApprovalInstance was actually created.
    private static CommitteeService MakeCommitteeService(InnovationDbContext db) =>
        new(db, new ApprovalService(db, new FakeAuditLogWriter(), new FakeNotificationService()));

    private static Guid SeedUser(SqliteContextFixture fixture, string samAccountName)
    {
        using var db = fixture.CreateContext();
        var id = Guid.NewGuid();
        db.Users.Add(new User { Id = id, SamAccountName = samAccountName, Email = $"{samAccountName}@gac-demo.sa", FullNameAr = samAccountName, FullNameEn = samAccountName });
        db.SaveChanges();
        return id;
    }

    private static void AssignJudgeRole(SqliteContextFixture fixture, Guid userId)
    {
        using var db = fixture.CreateContext();
        var judgeRoleId = db.Roles.Single(r => r.Code == RoleCodes.Judge).Id;
        db.Set<UserRole>().Add(new UserRole { UserId = userId, RoleId = judgeRoleId, IsPrimary = true });
        db.SaveChanges();
    }

    private static (LocalDiskFileStorage Storage, string RootPath) MakeStorage()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), $"committee-service-test-{Guid.NewGuid():N}");
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

    private static async Task<Idea> CreateIdeaInCommitteeAsync(SqliteContextFixture fixture, Guid submitterId, Guid activityId, Guid themeId)
    {
        var (storage, rootPath) = MakeStorage();
        try
        {
            using var db = fixture.CreateContext();
            var ideaService = MakeIdeaService(db, storage);
            var input = new IdeaInput("ا", "T", "م", "P", "ح", "S", "ف", "B", themeId, activityId, null, "individual", null, Array.Empty<TeamMemberInput>(), true, true);
            var created = await ideaService.CreateAsync(submitterId, input);

            var passStatus = db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.PassAwaitingAttachments);
            var idea = db.Ideas.Single(i => i.Id == created.Idea!.Id);
            idea.IdeaStatusId = passStatus.Id;
            db.SaveChanges();

            await ideaService.AddAttachmentAsync(created.Idea!.Id, submitterId, "a.pdf", "application/pdf", new byte[] { 1 });
            var submitted = await ideaService.SubmitToCommitteeAsync(created.Idea.Id, submitterId);
            return submitted.Idea!;
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }

    private static Dictionary<string, decimal> AllCriteriaScores(decimal score) => new()
    {
        ["originality"] = score,
        ["feasibility"] = score,
        ["impact"] = score,
        ["alignment"] = score,
    };

    [Fact]
    public async Task SubmitDecisionAsync_SingleJudge_ComputesWeightedScoreAndTransitionsToPendingFinalRanking()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedUser(fixture, "submitter1");
        var judgeId = SeedUser(fixture, "judge1");
        AssignJudgeRole(fixture, judgeId);
        using var themeDb = fixture.CreateContext();
        var themeId = themeDb.StrategicThemes.First().Id;
        var idea = await CreateIdeaInCommitteeAsync(fixture, submitterId, SeedActivity(fixture, submitterId), themeId);

        using var db = fixture.CreateContext();
        var service = MakeCommitteeService(db);
        var input = new CommitteeDecisionInput("approved", AllCriteriaScores(8), "Strong idea.");

        var result = await service.SubmitDecisionAsync(idea.Id, judgeId, input);

        Assert.Equal(CommitteeCommandStatus.Success, result.Status);
        Assert.Equal(8m, result.Decision!.TotalScore);
        Assert.Equal(8m, result.Idea!.CommitteeFinalScore);
        Assert.Equal(IdeaStatusCodes.PendingFinalRanking, result.Idea.IdeaStatus.Code);
    }

    [Fact]
    public async Task SubmitDecisionAsync_MultipleJudges_OnlyTransitionsAfterLastJudgeDecides()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedUser(fixture, "submitter1");
        var judge1Id = SeedUser(fixture, "judge1");
        var judge2Id = SeedUser(fixture, "judge2");
        AssignJudgeRole(fixture, judge1Id);
        AssignJudgeRole(fixture, judge2Id);
        using var themeDb = fixture.CreateContext();
        var themeId = themeDb.StrategicThemes.First().Id;
        var idea = await CreateIdeaInCommitteeAsync(fixture, submitterId, SeedActivity(fixture, submitterId), themeId);

        using var firstDb = fixture.CreateContext();
        var firstService = MakeCommitteeService(firstDb);
        var firstResult = await firstService.SubmitDecisionAsync(idea.Id, judge1Id, new CommitteeDecisionInput("approved", AllCriteriaScores(6), null));

        Assert.Equal(CommitteeCommandStatus.Success, firstResult.Status);
        Assert.Equal(IdeaStatusCodes.Committee, firstResult.Idea!.IdeaStatus.Code);

        using var secondDb = fixture.CreateContext();
        var secondService = MakeCommitteeService(secondDb);
        var secondResult = await secondService.SubmitDecisionAsync(idea.Id, judge2Id, new CommitteeDecisionInput("approved", AllCriteriaScores(10), null));

        Assert.Equal(CommitteeCommandStatus.Success, secondResult.Status);
        Assert.Equal(IdeaStatusCodes.PendingFinalRanking, secondResult.Idea!.IdeaStatus.Code);
        Assert.Equal(8m, secondResult.Idea.CommitteeFinalScore);
    }

    [Fact]
    public async Task SubmitDecisionAsync_DeferredDecision_StillCountsTowardConsensus()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedUser(fixture, "submitter1");
        var judgeId = SeedUser(fixture, "judge1");
        AssignJudgeRole(fixture, judgeId);
        using var themeDb = fixture.CreateContext();
        var themeId = themeDb.StrategicThemes.First().Id;
        var idea = await CreateIdeaInCommitteeAsync(fixture, submitterId, SeedActivity(fixture, submitterId), themeId);

        using var db = fixture.CreateContext();
        var service = MakeCommitteeService(db);
        var result = await service.SubmitDecisionAsync(idea.Id, judgeId, new CommitteeDecisionInput("deferred", AllCriteriaScores(5), null));

        Assert.Equal(CommitteeCommandStatus.Success, result.Status);
        Assert.Equal(IdeaStatusCodes.PendingFinalRanking, result.Idea!.IdeaStatus.Code);
    }

    [Fact]
    public async Task SubmitDecisionAsync_IdeaNotInCommitteeState_ReturnsInvalidState()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedUser(fixture, "submitter1");
        var judgeId = SeedUser(fixture, "judge1");
        AssignJudgeRole(fixture, judgeId);
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;
        var activityId = SeedActivity(fixture, submitterId);
        var (storage, rootPath) = MakeStorage();
        try
        {
            var ideaService = MakeIdeaService(db, storage);
            var created = await ideaService.CreateAsync(submitterId, new IdeaInput("ا", "T", "م", "P", "ح", "S", "ف", "B", themeId, activityId, null, "individual", null, Array.Empty<TeamMemberInput>(), true, true));

            var service = MakeCommitteeService(db);
            var result = await service.SubmitDecisionAsync(created.Idea!.Id, judgeId, new CommitteeDecisionInput("approved", AllCriteriaScores(8), null));

            Assert.Equal(CommitteeCommandStatus.InvalidState, result.Status);
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task SubmitDecisionAsync_AlreadyDecided_ReturnsAlreadyDecided()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedUser(fixture, "submitter1");
        var judge1Id = SeedUser(fixture, "judge1");
        var judge2Id = SeedUser(fixture, "judge2");
        AssignJudgeRole(fixture, judge1Id);
        AssignJudgeRole(fixture, judge2Id);
        using var themeDb = fixture.CreateContext();
        var themeId = themeDb.StrategicThemes.First().Id;
        var idea = await CreateIdeaInCommitteeAsync(fixture, submitterId, SeedActivity(fixture, submitterId), themeId);

        using var db = fixture.CreateContext();
        var service = MakeCommitteeService(db);
        await service.SubmitDecisionAsync(idea.Id, judge1Id, new CommitteeDecisionInput("approved", AllCriteriaScores(8), null));

        var retryResult = await service.SubmitDecisionAsync(idea.Id, judge1Id, new CommitteeDecisionInput("approved", AllCriteriaScores(9), null));

        Assert.Equal(CommitteeCommandStatus.AlreadyDecided, retryResult.Status);
    }

    [Fact]
    public async Task SubmitDecisionAsync_InvalidDecisionTypeCode_ReturnsInvalidDecisionType()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedUser(fixture, "submitter1");
        var judgeId = SeedUser(fixture, "judge1");
        AssignJudgeRole(fixture, judgeId);
        using var themeDb = fixture.CreateContext();
        var themeId = themeDb.StrategicThemes.First().Id;
        var idea = await CreateIdeaInCommitteeAsync(fixture, submitterId, SeedActivity(fixture, submitterId), themeId);

        using var db = fixture.CreateContext();
        var service = MakeCommitteeService(db);
        var result = await service.SubmitDecisionAsync(idea.Id, judgeId, new CommitteeDecisionInput("maybe", AllCriteriaScores(8), null));

        Assert.Equal(CommitteeCommandStatus.InvalidDecisionType, result.Status);
    }

    [Fact]
    public async Task SubmitDecisionAsync_MissingCriterion_ReturnsInvalidCriteria()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedUser(fixture, "submitter1");
        var judgeId = SeedUser(fixture, "judge1");
        AssignJudgeRole(fixture, judgeId);
        using var themeDb = fixture.CreateContext();
        var themeId = themeDb.StrategicThemes.First().Id;
        var idea = await CreateIdeaInCommitteeAsync(fixture, submitterId, SeedActivity(fixture, submitterId), themeId);

        using var db = fixture.CreateContext();
        var service = MakeCommitteeService(db);
        var incompleteScores = new Dictionary<string, decimal> { ["originality"] = 8, ["feasibility"] = 8, ["impact"] = 8 };
        var result = await service.SubmitDecisionAsync(idea.Id, judgeId, new CommitteeDecisionInput("approved", incompleteScores, null));

        Assert.Equal(CommitteeCommandStatus.InvalidCriteria, result.Status);
    }

    [Fact]
    public async Task SubmitDecisionAsync_ScoreOutOfRange_ReturnsInvalidCriteria()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedUser(fixture, "submitter1");
        var judgeId = SeedUser(fixture, "judge1");
        AssignJudgeRole(fixture, judgeId);
        using var themeDb = fixture.CreateContext();
        var themeId = themeDb.StrategicThemes.First().Id;
        var idea = await CreateIdeaInCommitteeAsync(fixture, submitterId, SeedActivity(fixture, submitterId), themeId);

        using var db = fixture.CreateContext();
        var service = MakeCommitteeService(db);
        var result = await service.SubmitDecisionAsync(idea.Id, judgeId, new CommitteeDecisionInput("approved", AllCriteriaScores(11), null));

        Assert.Equal(CommitteeCommandStatus.InvalidCriteria, result.Status);
    }

    [Fact]
    public async Task GetQueueAsync_ReturnsCommitteeIdeas_WithDecidedCountAndHasDecidedFlag()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedUser(fixture, "submitter1");
        var judge1Id = SeedUser(fixture, "judge1");
        var judge2Id = SeedUser(fixture, "judge2");
        AssignJudgeRole(fixture, judge1Id);
        AssignJudgeRole(fixture, judge2Id);
        using var themeDb = fixture.CreateContext();
        var themeId = themeDb.StrategicThemes.First().Id;
        var idea = await CreateIdeaInCommitteeAsync(fixture, submitterId, SeedActivity(fixture, submitterId), themeId);

        using var decideDb = fixture.CreateContext();
        var decideService = MakeCommitteeService(decideDb);
        await decideService.SubmitDecisionAsync(idea.Id, judge1Id, new CommitteeDecisionInput("approved", AllCriteriaScores(8), null));

        using var db = fixture.CreateContext();
        var service = MakeCommitteeService(db);
        var queueForJudge1 = await service.GetQueueAsync(judge1Id);
        var queueForJudge2 = await service.GetQueueAsync(judge2Id);

        Assert.Single(queueForJudge1);
        Assert.True(queueForJudge1[0].HasDecided);
        Assert.Equal(1, queueForJudge1[0].DecidedCount);
        Assert.Equal(2, queueForJudge1[0].TotalJudges);

        Assert.Single(queueForJudge2);
        Assert.False(queueForJudge2[0].HasDecided);
    }

    [Fact]
    public async Task GetMyDecisionsAsync_ReturnsOnlyCallersDecisions()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedUser(fixture, "submitter1");
        var judge1Id = SeedUser(fixture, "judge1");
        var judge2Id = SeedUser(fixture, "judge2");
        AssignJudgeRole(fixture, judge1Id);
        AssignJudgeRole(fixture, judge2Id);
        using var themeDb = fixture.CreateContext();
        var themeId = themeDb.StrategicThemes.First().Id;
        var idea = await CreateIdeaInCommitteeAsync(fixture, submitterId, SeedActivity(fixture, submitterId), themeId);

        using var db = fixture.CreateContext();
        var service = MakeCommitteeService(db);
        await service.SubmitDecisionAsync(idea.Id, judge1Id, new CommitteeDecisionInput("approved", AllCriteriaScores(8), null));

        var judge1Decisions = await service.GetMyDecisionsAsync(judge1Id);
        var judge2Decisions = await service.GetMyDecisionsAsync(judge2Id);

        Assert.Single(judge1Decisions);
        Assert.Empty(judge2Decisions);
    }

    [Fact]
    public async Task SubmitDecisionAsync_FinalJudgeDecision_OpensCommitteePublishApprovalInstanceForIdea()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedUser(fixture, "submitter1");
        var judge1Id = SeedUser(fixture, "judge1");
        var judge2Id = SeedUser(fixture, "judge2");
        AssignJudgeRole(fixture, judge1Id);
        AssignJudgeRole(fixture, judge2Id);
        using var themeDb = fixture.CreateContext();
        var themeId = themeDb.StrategicThemes.First().Id;
        var idea = await CreateIdeaInCommitteeAsync(fixture, submitterId, SeedActivity(fixture, submitterId), themeId);

        using var firstDb = fixture.CreateContext();
        var firstService = MakeCommitteeService(firstDb);
        await firstService.SubmitDecisionAsync(idea.Id, judge1Id, new CommitteeDecisionInput("approved", AllCriteriaScores(6), null));

        using var verifyBeforeDb = fixture.CreateContext();
        Assert.False(await verifyBeforeDb.ApprovalInstances.AnyAsync(i => i.EntityType == "committee_decision" && i.EntityId == idea.Id));

        using var secondDb = fixture.CreateContext();
        var secondService = MakeCommitteeService(secondDb);
        var secondResult = await secondService.SubmitDecisionAsync(idea.Id, judge2Id, new CommitteeDecisionInput("approved", AllCriteriaScores(10), null));
        Assert.Equal(IdeaStatusCodes.PendingFinalRanking, secondResult.Idea!.IdeaStatus.Code);

        using var verifyDb = fixture.CreateContext();
        var instance = await verifyDb.ApprovalInstances
            .Include(i => i.ApprovalChain)
            .Include(i => i.ApprovalInstanceStatus)
            .SingleAsync(i => i.EntityType == "committee_decision" && i.EntityId == idea.Id);
        Assert.Equal("committee-publish", instance.ApprovalChain.Code);
        Assert.Equal("pending", instance.ApprovalInstanceStatus.Code);
    }
}
