using InnovationToImpact.Domain.Approvals;
using InnovationToImpact.Domain.Audit;
using InnovationToImpact.Domain.Auth;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Notifications;
using InnovationToImpact.Infrastructure.Approvals;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class ApprovalServiceTests
{
    private const string CommitteePublishChain = "committee-publish";
    private const string IdeaApproveChain = "idea-approve";

    private sealed class FakeAuditLogWriter : IAuditLogWriter
    {
        public List<(string EntityType, Guid EntityId, string Action, Guid? ActorId, string? Payload)> Calls { get; } = new();

        public Task<AuditLog> AppendAsync(string entityType, Guid entityId, string action, Guid? actorId, string? payload, CancellationToken cancellationToken = default)
        {
            Calls.Add((entityType, entityId, action, actorId, payload));
            return Task.FromResult(new AuditLog { Id = Guid.NewGuid(), EntityType = entityType, EntityId = entityId, Action = action, ActorId = actorId, Payload = payload });
        }
    }

    private sealed class FakeNotificationService : INotificationService
    {
        public List<(Guid UserId, string Type)> Calls { get; } = new();

        public Task<Notification> CreateAndPublishAsync(Guid userId, string notificationType, string titleAr, string titleEn, string bodyAr, string bodyEn, string? link, string? payloadJson, CancellationToken cancellationToken = default)
        {
            Calls.Add((userId, notificationType));
            return Task.FromResult(new Notification { Id = Guid.NewGuid(), UserId = userId, NotificationType = notificationType, TitleAr = titleAr, TitleEn = titleEn, BodyAr = bodyAr, BodyEn = bodyEn });
        }
    }

    private static Guid SeedUserWithRole(SqliteContextFixture fixture, string samAccountName, string roleCode)
    {
        using var db = fixture.CreateContext();
        var roleId = db.Roles.Single(r => r.Code == roleCode).Id;
        var id = Guid.NewGuid();
        db.Users.Add(new User { Id = id, SamAccountName = samAccountName, Email = $"{samAccountName}@gac-demo.sa", FullNameAr = samAccountName, FullNameEn = samAccountName });
        db.SaveChanges();
        db.Set<UserRole>().Add(new UserRole { UserId = id, RoleId = roleId, IsPrimary = true });
        db.SaveChanges();
        return id;
    }

    private static ApprovalService CreateService(SqliteContextFixture fixture, out FakeAuditLogWriter audit, out FakeNotificationService notifications)
    {
        audit = new FakeAuditLogWriter();
        notifications = new FakeNotificationService();
        return new ApprovalService(fixture.CreateContext(), audit, notifications);
    }

    [Fact]
    public async Task OpenInstanceAsync_NoExistingInstance_CreatesPendingInstance()
    {
        using var fixture = new SqliteContextFixture();
        var service = CreateService(fixture, out _, out _);
        var entityId = Guid.NewGuid();

        await service.OpenInstanceAsync(CommitteePublishChain, "committee_decision", entityId);

        using var verifyDb = fixture.CreateContext();
        var instance = await verifyDb.ApprovalInstances
            .Include(i => i.ApprovalInstanceStatus)
            .SingleAsync(i => i.EntityType == "committee_decision" && i.EntityId == entityId);
        Assert.Equal("pending", instance.ApprovalInstanceStatus.Code);
        Assert.Equal(1, instance.CurrentStepOrder);
    }

    [Fact]
    public async Task OpenInstanceAsync_CalledTwiceForSameTriple_IsIdempotent()
    {
        using var fixture = new SqliteContextFixture();
        var service = CreateService(fixture, out _, out _);
        var entityId = Guid.NewGuid();

        await service.OpenInstanceAsync(CommitteePublishChain, "committee_decision", entityId);
        await service.OpenInstanceAsync(CommitteePublishChain, "committee_decision", entityId);

        using var verifyDb = fixture.CreateContext();
        var count = await verifyDb.ApprovalInstances.CountAsync(i => i.EntityType == "committee_decision" && i.EntityId == entityId);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task OpenInstanceAsync_UnknownChainCode_DoesNotThrowAndCreatesNothing()
    {
        using var fixture = new SqliteContextFixture();
        var service = CreateService(fixture, out _, out _);
        var entityId = Guid.NewGuid();

        await service.OpenInstanceAsync("no-such-chain", "committee_decision", entityId);

        using var verifyDb = fixture.CreateContext();
        Assert.Equal(0, await verifyDb.ApprovalInstances.CountAsync());
    }

    [Fact]
    public async Task OpenInstanceAsync_NotifiesStep1RoleHolders()
    {
        using var fixture = new SqliteContextFixture();
        var evaluatorId = SeedUserWithRole(fixture, "eval-notify", RoleCodes.Evaluator);
        var service = CreateService(fixture, out _, out var notifications);

        await service.OpenInstanceAsync(CommitteePublishChain, "committee_decision", Guid.NewGuid());

        Assert.Contains(notifications.Calls, c => c.UserId == evaluatorId && c.Type == "approval_requested");
    }

    [Fact]
    public async Task GetPendingForUserAsync_EvaluatorSeesStep1_JudgeDoesNot()
    {
        using var fixture = new SqliteContextFixture();
        var evaluatorId = SeedUserWithRole(fixture, "eval1", RoleCodes.Evaluator);
        var judgeId = SeedUserWithRole(fixture, "judge1", RoleCodes.Judge);
        var service = CreateService(fixture, out _, out _);
        var entityId = Guid.NewGuid();
        await service.OpenInstanceAsync(CommitteePublishChain, "committee_decision", entityId);

        var evaluatorCards = await service.GetPendingForUserAsync(evaluatorId, new[] { RoleCodes.Evaluator });
        var judgeCards = await service.GetPendingForUserAsync(judgeId, new[] { RoleCodes.Judge });

        var card = Assert.Single(evaluatorCards);
        Assert.Equal(1, card.StepOrder);
        Assert.Equal(entityId, card.EntityId);
        Assert.Equal(0, card.PriorApprovers);
        Assert.Empty(judgeCards);
    }

    [Fact]
    public async Task GetPendingForUserAsync_AfterStep1Approved_JudgeSeesStep2()
    {
        using var fixture = new SqliteContextFixture();
        var evaluatorId = SeedUserWithRole(fixture, "eval2", RoleCodes.Evaluator);
        var judgeId = SeedUserWithRole(fixture, "judge2", RoleCodes.Judge);
        var service = CreateService(fixture, out _, out _);
        var entityId = Guid.NewGuid();
        await service.OpenInstanceAsync(CommitteePublishChain, "committee_decision", entityId);

        var evaluatorCards = await service.GetPendingForUserAsync(evaluatorId, new[] { RoleCodes.Evaluator });
        var step1 = evaluatorCards.Single();
        await service.RecordDecisionAsync(step1.InstanceId, step1.StepId, evaluatorId, new[] { RoleCodes.Evaluator }, "approve", "looks good");

        var judgeCards = await service.GetPendingForUserAsync(judgeId, new[] { RoleCodes.Judge });

        var card = Assert.Single(judgeCards);
        Assert.Equal(2, card.StepOrder);
        Assert.Equal(0, card.PriorApprovers);
    }

    [Fact]
    public async Task GetPendingForUserAsync_UserAlreadyDecidedStep_DoesNotSeeCardAgain()
    {
        using var fixture = new SqliteContextFixture();
        var judge1 = SeedUserWithRole(fixture, "judge-a", RoleCodes.Judge);
        var judge2 = SeedUserWithRole(fixture, "judge-b", RoleCodes.Judge);
        var evaluatorId = SeedUserWithRole(fixture, "eval3", RoleCodes.Evaluator);
        var service = CreateService(fixture, out _, out _);
        var entityId = Guid.NewGuid();
        await service.OpenInstanceAsync(CommitteePublishChain, "committee_decision", entityId);
        var step1 = (await service.GetPendingForUserAsync(evaluatorId, new[] { RoleCodes.Evaluator })).Single();
        await service.RecordDecisionAsync(step1.InstanceId, step1.StepId, evaluatorId, new[] { RoleCodes.Evaluator }, "approve", null);

        var step2Card = (await service.GetPendingForUserAsync(judge1, new[] { RoleCodes.Judge })).Single();
        await service.RecordDecisionAsync(step2Card.InstanceId, step2Card.StepId, judge1, new[] { RoleCodes.Judge }, "approve", null);

        var judge1CardsAfter = await service.GetPendingForUserAsync(judge1, new[] { RoleCodes.Judge });
        var judge2CardsAfter = await service.GetPendingForUserAsync(judge2, new[] { RoleCodes.Judge });

        Assert.Empty(judge1CardsAfter);
        var judge2Card = Assert.Single(judge2CardsAfter);
        Assert.Equal(1, judge2Card.PriorApprovers);
    }

    [Fact]
    public async Task RecordDecisionAsync_ApproveStep1Min1_AdvancesToStep2AndStaysPending()
    {
        using var fixture = new SqliteContextFixture();
        var evaluatorId = SeedUserWithRole(fixture, "eval4", RoleCodes.Evaluator);
        var service = CreateService(fixture, out var audit, out _);
        var entityId = Guid.NewGuid();
        await service.OpenInstanceAsync(CommitteePublishChain, "committee_decision", entityId);
        var step1 = (await service.GetPendingForUserAsync(evaluatorId, new[] { RoleCodes.Evaluator })).Single();

        var result = await service.RecordDecisionAsync(step1.InstanceId, step1.StepId, evaluatorId, new[] { RoleCodes.Evaluator }, "approve", "ok");

        Assert.Equal(ApprovalCommandStatus.Success, result.Status);
        Assert.Equal("pending", result.Instance!.ApprovalInstanceStatus.Code);
        Assert.Equal(2, result.Instance.CurrentStepOrder);
        Assert.Contains(audit.Calls, c => c.Action == "approval.approve" && c.EntityId == step1.InstanceId);
    }

    [Fact]
    public async Task RecordDecisionAsync_Reject_ClosesInstanceImmediately()
    {
        using var fixture = new SqliteContextFixture();
        var evaluatorId = SeedUserWithRole(fixture, "eval5", RoleCodes.Evaluator);
        var service = CreateService(fixture, out var audit, out _);
        var entityId = Guid.NewGuid();
        await service.OpenInstanceAsync(CommitteePublishChain, "committee_decision", entityId);
        var step1 = (await service.GetPendingForUserAsync(evaluatorId, new[] { RoleCodes.Evaluator })).Single();

        var result = await service.RecordDecisionAsync(step1.InstanceId, step1.StepId, evaluatorId, new[] { RoleCodes.Evaluator }, "reject", "no good");

        Assert.Equal(ApprovalCommandStatus.Success, result.Status);
        Assert.Equal("rejected", result.Instance!.ApprovalInstanceStatus.Code);
        Assert.NotNull(result.Instance.CompletedAt);
        Assert.Contains(audit.Calls, c => c.Action == "approval.reject" && c.EntityId == step1.InstanceId);
    }

    [Fact]
    public async Task RecordDecisionAsync_Step2ReachesMinApprovers_CompletesInstanceApproved()
    {
        using var fixture = new SqliteContextFixture();
        var evaluatorId = SeedUserWithRole(fixture, "eval6", RoleCodes.Evaluator);
        var judge1 = SeedUserWithRole(fixture, "judge-c", RoleCodes.Judge);
        var judge2 = SeedUserWithRole(fixture, "judge-d", RoleCodes.Judge);
        var service = CreateService(fixture, out _, out _);
        var entityId = Guid.NewGuid();
        await service.OpenInstanceAsync(CommitteePublishChain, "committee_decision", entityId);
        var step1 = (await service.GetPendingForUserAsync(evaluatorId, new[] { RoleCodes.Evaluator })).Single();
        await service.RecordDecisionAsync(step1.InstanceId, step1.StepId, evaluatorId, new[] { RoleCodes.Evaluator }, "approve", null);

        var step2Card = (await service.GetPendingForUserAsync(judge1, new[] { RoleCodes.Judge })).Single();
        var firstJudgeResult = await service.RecordDecisionAsync(step2Card.InstanceId, step2Card.StepId, judge1, new[] { RoleCodes.Judge }, "approve", null);
        Assert.Equal("pending", firstJudgeResult.Instance!.ApprovalInstanceStatus.Code);

        var secondJudgeResult = await service.RecordDecisionAsync(step2Card.InstanceId, step2Card.StepId, judge2, new[] { RoleCodes.Judge }, "approve", null);

        Assert.Equal(ApprovalCommandStatus.Success, secondJudgeResult.Status);
        Assert.Equal("approved", secondJudgeResult.Instance!.ApprovalInstanceStatus.Code);
        Assert.NotNull(secondJudgeResult.Instance.CompletedAt);
    }

    [Fact]
    public async Task RecordDecisionAsync_WrongRole_ReturnsForbidden()
    {
        using var fixture = new SqliteContextFixture();
        var evaluatorId = SeedUserWithRole(fixture, "eval7", RoleCodes.Evaluator);
        var judgeId = SeedUserWithRole(fixture, "judge-e", RoleCodes.Judge);
        var service = CreateService(fixture, out _, out _);
        var entityId = Guid.NewGuid();
        await service.OpenInstanceAsync(CommitteePublishChain, "committee_decision", entityId);
        var step1 = (await service.GetPendingForUserAsync(evaluatorId, new[] { RoleCodes.Evaluator })).Single();

        var result = await service.RecordDecisionAsync(step1.InstanceId, step1.StepId, judgeId, new[] { RoleCodes.Judge }, "approve", null);

        Assert.Equal(ApprovalCommandStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task RecordDecisionAsync_SameUserDecidesTwice_ReturnsInvalidState()
    {
        using var fixture = new SqliteContextFixture();
        var judge1 = SeedUserWithRole(fixture, "judge-f", RoleCodes.Judge);
        var judge2 = SeedUserWithRole(fixture, "judge-g", RoleCodes.Judge);
        var evaluatorId = SeedUserWithRole(fixture, "eval8", RoleCodes.Evaluator);
        var service = CreateService(fixture, out _, out _);
        var entityId = Guid.NewGuid();
        await service.OpenInstanceAsync(CommitteePublishChain, "committee_decision", entityId);
        var step1 = (await service.GetPendingForUserAsync(evaluatorId, new[] { RoleCodes.Evaluator })).Single();
        await service.RecordDecisionAsync(step1.InstanceId, step1.StepId, evaluatorId, new[] { RoleCodes.Evaluator }, "approve", null);
        var step2Card = (await service.GetPendingForUserAsync(judge1, new[] { RoleCodes.Judge })).Single();
        await service.RecordDecisionAsync(step2Card.InstanceId, step2Card.StepId, judge1, new[] { RoleCodes.Judge }, "approve", null);

        var result = await service.RecordDecisionAsync(step2Card.InstanceId, step2Card.StepId, judge1, new[] { RoleCodes.Judge }, "approve", null);

        Assert.Equal(ApprovalCommandStatus.InvalidState, result.Status);
        // second distinct judge is unaffected
        _ = judge2;
    }

    [Fact]
    public async Task RecordDecisionAsync_NonPendingInstance_ReturnsInvalidState()
    {
        using var fixture = new SqliteContextFixture();
        var evaluatorId = SeedUserWithRole(fixture, "eval9", RoleCodes.Evaluator);
        var service = CreateService(fixture, out _, out _);
        var entityId = Guid.NewGuid();
        await service.OpenInstanceAsync(CommitteePublishChain, "committee_decision", entityId);
        var step1 = (await service.GetPendingForUserAsync(evaluatorId, new[] { RoleCodes.Evaluator })).Single();
        await service.RecordDecisionAsync(step1.InstanceId, step1.StepId, evaluatorId, new[] { RoleCodes.Evaluator }, "reject", null);

        var result = await service.RecordDecisionAsync(step1.InstanceId, step1.StepId, evaluatorId, new[] { RoleCodes.Evaluator }, "approve", null);

        Assert.Equal(ApprovalCommandStatus.InvalidState, result.Status);
    }

    [Fact]
    public async Task BulkDecideAsync_TwoValidTargets_ReturnsSucceededTwo()
    {
        using var fixture = new SqliteContextFixture();
        var evaluatorId = SeedUserWithRole(fixture, "eval10", RoleCodes.Evaluator);
        var service = CreateService(fixture, out _, out _);
        var entityId1 = Guid.NewGuid();
        var entityId2 = Guid.NewGuid();
        await service.OpenInstanceAsync(CommitteePublishChain, "committee_decision", entityId1);
        await service.OpenInstanceAsync(CommitteePublishChain, "committee_decision", entityId2);
        var cards = await service.GetPendingForUserAsync(evaluatorId, new[] { RoleCodes.Evaluator });
        Assert.Equal(2, cards.Count);
        var targets = cards.Select(c => (c.InstanceId, c.StepId)).ToList();

        var (succeeded, failed) = await service.BulkDecideAsync(targets, evaluatorId, new[] { RoleCodes.Evaluator }, "approve", "batch ok");

        Assert.Equal(2, succeeded);
        Assert.Empty(failed);
    }

    [Fact]
    public async Task BulkDecideAsync_OneInvalidTarget_ReturnsItInFailedInstanceIds()
    {
        using var fixture = new SqliteContextFixture();
        var evaluatorId = SeedUserWithRole(fixture, "eval11", RoleCodes.Evaluator);
        var service = CreateService(fixture, out _, out _);
        var entityId = Guid.NewGuid();
        await service.OpenInstanceAsync(CommitteePublishChain, "committee_decision", entityId);
        var card = (await service.GetPendingForUserAsync(evaluatorId, new[] { RoleCodes.Evaluator })).Single();
        var badInstanceId = Guid.NewGuid();
        var targets = new List<(Guid InstanceId, Guid StepId)> { (card.InstanceId, card.StepId), (badInstanceId, card.StepId) };

        var (succeeded, failed) = await service.BulkDecideAsync(targets, evaluatorId, new[] { RoleCodes.Evaluator }, "approve", null);

        Assert.Equal(1, succeeded);
        var onlyFailed = Assert.Single(failed);
        Assert.Equal(badInstanceId, onlyFailed);
    }

    [Fact]
    public async Task OpenInstanceAsync_IdeaApproveChain_AdminSeesStep1()
    {
        using var fixture = new SqliteContextFixture();
        var adminId = SeedUserWithRole(fixture, "admin1", RoleCodes.Admin);
        var service = CreateService(fixture, out _, out _);
        var entityId = Guid.NewGuid();

        await service.OpenInstanceAsync(IdeaApproveChain, "idea", entityId);
        var cards = await service.GetPendingForUserAsync(adminId, new[] { RoleCodes.Admin });

        var card = Assert.Single(cards);
        Assert.Equal("idea", card.EntityType);
        Assert.Equal(1, card.MinApprovers);
    }
}
