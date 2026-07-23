using System.Text.Json;
using InnovationToImpact.Domain.Approvals;
using InnovationToImpact.Domain.Audit;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Notifications;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Approvals;

public class ApprovalService : IApprovalService
{
    private readonly InnovationDbContext _db;
    private readonly IAuditLogWriter _auditLogWriter;
    private readonly INotificationService _notificationService;

    public ApprovalService(InnovationDbContext db, IAuditLogWriter auditLogWriter, INotificationService notificationService)
    {
        _db = db;
        _auditLogWriter = auditLogWriter;
        _notificationService = notificationService;
    }

    public async Task<IReadOnlyList<PendingApprovalCard>> GetPendingForUserAsync(Guid userId, IReadOnlyCollection<string> roleCodes, CancellationToken cancellationToken = default)
    {
        var pendingStatusId = await GetInstanceStatusIdAsync("pending", cancellationToken);
        var approveTypeId = await GetDecisionTypeIdAsync("approve", cancellationToken);

        var instances = await _db.ApprovalInstances
            .Include(i => i.ApprovalChain)
            .Where(i => i.ApprovalInstanceStatusId == pendingStatusId)
            .ToListAsync(cancellationToken);

        var cards = new List<PendingApprovalCard>();
        foreach (var instance in instances)
        {
            var steps = await LoadStepsAsync(instance.ApprovalChainId, cancellationToken);
            var decisions = await LoadDecisionsAsync(instance.Id, cancellationToken);
            var pendingStep = ComputePendingStep(steps, decisions, approveTypeId);
            if (pendingStep is null) continue;
            if (!roleCodes.Contains(pendingStep.Role.Code)) continue;
            if (decisions.Any(d => d.ApprovalChainStepId == pendingStep.Id && d.DeciderId == userId)) continue;

            var priorApprovers = decisions.Count(d => d.ApprovalChainStepId == pendingStep.Id && d.ApprovalDecisionTypeId == approveTypeId);
            cards.Add(new PendingApprovalCard(
                instance.Id,
                pendingStep.Id,
                instance.EntityType,
                instance.EntityId,
                instance.ApprovalChain.NameAr,
                instance.ApprovalChain.NameEn,
                pendingStep.LabelAr,
                pendingStep.LabelEn,
                pendingStep.StepOrder,
                pendingStep.MinApprovers,
                priorApprovers));
        }

        return cards;
    }

    public async Task<ApprovalCommandResult> RecordDecisionAsync(
        Guid instanceId,
        Guid stepId,
        Guid deciderId,
        IReadOnlyCollection<string> deciderRoles,
        string decision,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        var instance = await _db.ApprovalInstances
            .Include(i => i.ApprovalChain)
            .Include(i => i.ApprovalInstanceStatus)
            .SingleOrDefaultAsync(i => i.Id == instanceId, cancellationToken);
        if (instance is null) return new ApprovalCommandResult(ApprovalCommandStatus.NotFound, null);
        if (instance.ApprovalInstanceStatus.Code != "pending") return new ApprovalCommandResult(ApprovalCommandStatus.InvalidState, null);

        var approveTypeId = await GetDecisionTypeIdAsync("approve", cancellationToken);
        var steps = await LoadStepsAsync(instance.ApprovalChainId, cancellationToken);
        var decisions = await LoadDecisionsAsync(instance.Id, cancellationToken);
        var pendingStep = ComputePendingStep(steps, decisions, approveTypeId);

        if (pendingStep is null || pendingStep.Id != stepId) return new ApprovalCommandResult(ApprovalCommandStatus.InvalidState, null);
        if (!deciderRoles.Contains(pendingStep.Role.Code)) return new ApprovalCommandResult(ApprovalCommandStatus.Forbidden, null);
        if (decisions.Any(d => d.ApprovalChainStepId == stepId && d.DeciderId == deciderId)) return new ApprovalCommandResult(ApprovalCommandStatus.InvalidState, null);

        var decisionTypeId = await GetDecisionTypeIdAsync(decision, cancellationToken);
        var newDecision = new ApprovalStepDecision
        {
            Id = Guid.NewGuid(),
            ApprovalInstanceId = instanceId,
            ApprovalChainStepId = stepId,
            DeciderId = deciderId,
            ApprovalDecisionTypeId = decisionTypeId,
            CommentsAr = comment,
            CommentsEn = comment,
            DecidedAt = DateTime.UtcNow,
        };
        _db.ApprovalStepDecisions.Add(newDecision);

        ApprovalChainStep? notifyStep = null;

        if (decision == "reject")
        {
            instance.ApprovalInstanceStatusId = await GetInstanceStatusIdAsync("rejected", cancellationToken);
            instance.CompletedAt = DateTime.UtcNow;
        }
        else
        {
            decisions.Add(newDecision);
            var newPendingStep = ComputePendingStep(steps, decisions, approveTypeId);
            if (newPendingStep is null)
            {
                instance.ApprovalInstanceStatusId = await GetInstanceStatusIdAsync("approved", cancellationToken);
                instance.CompletedAt = DateTime.UtcNow;
            }
            else
            {
                instance.CurrentStepOrder = newPendingStep.StepOrder;
                notifyStep = newPendingStep;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        var action = decision == "reject" ? "approval.reject" : "approval.approve";
        await _auditLogWriter.AppendAsync(
            "approval_instance",
            instanceId,
            action,
            deciderId,
            JsonSerializer.Serialize(new { stepId, decision, comment }),
            cancellationToken);

        if (notifyStep is not null)
        {
            await NotifyRoleHoldersAsync(notifyStep.Role.Code, cancellationToken);
        }

        await _db.Entry(instance).Reference(i => i.ApprovalInstanceStatus).LoadAsync(cancellationToken);

        return new ApprovalCommandResult(ApprovalCommandStatus.Success, instance);
    }

    public async Task<(int Succeeded, IReadOnlyList<Guid> FailedInstanceIds)> BulkDecideAsync(
        IReadOnlyList<(Guid InstanceId, Guid StepId)> targets,
        Guid deciderId,
        IReadOnlyCollection<string> deciderRoles,
        string decision,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        var succeeded = 0;
        var failed = new List<Guid>();

        foreach (var (instanceId, stepId) in targets)
        {
            var result = await RecordDecisionAsync(instanceId, stepId, deciderId, deciderRoles, decision, comment, cancellationToken);
            if (result.Status == ApprovalCommandStatus.Success)
            {
                succeeded++;
            }
            else
            {
                failed.Add(instanceId);
            }
        }

        return (succeeded, failed);
    }

    public async Task OpenInstanceAsync(string chainCode, string entityType, Guid entityId, CancellationToken cancellationToken = default)
    {
        try
        {
            var chain = await _db.ApprovalChains.SingleOrDefaultAsync(c => c.Code == chainCode && c.IsActive, cancellationToken);
            if (chain is null) return;

            var pendingStatusId = await GetInstanceStatusIdAsync("pending", cancellationToken);
            var alreadyOpen = await _db.ApprovalInstances.AnyAsync(
                i => i.ApprovalChainId == chain.Id && i.EntityType == entityType && i.EntityId == entityId && i.ApprovalInstanceStatusId == pendingStatusId,
                cancellationToken);
            if (alreadyOpen) return;

            var instance = new ApprovalInstance
            {
                Id = Guid.NewGuid(),
                ApprovalChainId = chain.Id,
                EntityType = entityType,
                EntityId = entityId,
                ApprovalInstanceStatusId = pendingStatusId,
                CurrentStepOrder = 1,
                StartedAt = DateTime.UtcNow,
            };
            _db.ApprovalInstances.Add(instance);
            await _db.SaveChangesAsync(cancellationToken);

            var firstStep = (await LoadStepsAsync(chain.Id, cancellationToken)).FirstOrDefault();
            if (firstStep is not null)
            {
                await NotifyRoleHoldersAsync(firstStep.Role.Code, cancellationToken);
            }

            await _auditLogWriter.AppendAsync(
                "approval_instance",
                instance.Id,
                "approval.opened",
                null,
                JsonSerializer.Serialize(new { chainCode, entityType, entityId }),
                cancellationToken);
        }
        catch
        {
            // A gate failure must never break the caller's own transition.
        }
    }

    private async Task<List<ApprovalChainStep>> LoadStepsAsync(Guid chainId, CancellationToken cancellationToken) =>
        await _db.ApprovalChainSteps
            .Include(s => s.Role)
            .Where(s => s.ApprovalChainId == chainId)
            .OrderBy(s => s.StepOrder)
            .ToListAsync(cancellationToken);

    private async Task<List<ApprovalStepDecision>> LoadDecisionsAsync(Guid instanceId, CancellationToken cancellationToken) =>
        await _db.ApprovalStepDecisions
            .Where(d => d.ApprovalInstanceId == instanceId)
            .ToListAsync(cancellationToken);

    private static ApprovalChainStep? ComputePendingStep(List<ApprovalChainStep> steps, List<ApprovalStepDecision> decisions, Guid approveTypeId)
    {
        foreach (var step in steps)
        {
            var approveCount = decisions.Count(d => d.ApprovalChainStepId == step.Id && d.ApprovalDecisionTypeId == approveTypeId);
            if (approveCount < step.MinApprovers) return step;
        }

        return null;
    }

    private async Task<Guid> GetInstanceStatusIdAsync(string code, CancellationToken cancellationToken) =>
        await _db.ApprovalInstanceStatuses.Where(s => s.Code == code).Select(s => s.Id).SingleAsync(cancellationToken);

    private async Task<Guid> GetDecisionTypeIdAsync(string code, CancellationToken cancellationToken) =>
        await _db.ApprovalDecisionTypes.Where(d => d.Code == code).Select(d => d.Id).SingleAsync(cancellationToken);

    private async Task NotifyRoleHoldersAsync(string roleCode, CancellationToken cancellationToken)
    {
        var userIds = await _db.Users
            .Where(u => u.UserRoles.Any(ur => ur.Role.Code == roleCode))
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        foreach (var userId in userIds)
        {
            await _notificationService.CreateAndPublishAsync(
                userId,
                "approval_requested",
                "طلب موافقة",
                "Approval requested",
                bodyAr: "لديك طلب موافقة جديد بانتظار قرارك.",
                bodyEn: "You have a new approval request awaiting your decision.",
                link: "/approvals",
                payloadJson: null,
                cancellationToken);
        }
    }
}
