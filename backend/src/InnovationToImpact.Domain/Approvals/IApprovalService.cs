using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.Approvals;

public interface IApprovalService
{
    Task<IReadOnlyList<PendingApprovalCard>> GetPendingForUserAsync(Guid userId, IReadOnlyCollection<string> roleCodes, CancellationToken cancellationToken = default);

    Task<ApprovalCommandResult> RecordDecisionAsync(
        Guid instanceId,
        Guid stepId,
        Guid deciderId,
        IReadOnlyCollection<string> deciderRoles,
        string decision,
        string? comment,
        CancellationToken cancellationToken = default);

    Task<(int Succeeded, IReadOnlyList<Guid> FailedInstanceIds)> BulkDecideAsync(
        IReadOnlyList<(Guid InstanceId, Guid StepId)> targets,
        Guid deciderId,
        IReadOnlyCollection<string> deciderRoles,
        string decision,
        string? comment,
        CancellationToken cancellationToken = default);

    Task OpenInstanceAsync(string chainCode, string entityType, Guid entityId, CancellationToken cancellationToken = default);
}

public sealed record ApprovalDecideInput(Guid InstanceId, Guid StepId, string Decision, string? Comment);

public sealed record ApprovalBulkTarget(Guid InstanceId, Guid StepId);

public sealed record ApprovalBulkInput(IReadOnlyList<ApprovalBulkTarget> Targets, string Decision, string? Comment);

public sealed record PendingApprovalCard(
    Guid InstanceId,
    Guid StepId,
    string EntityType,
    Guid EntityId,
    string ChainNameAr,
    string ChainNameEn,
    string StepLabelAr,
    string StepLabelEn,
    int StepOrder,
    int MinApprovers,
    int PriorApprovers);

public sealed record ApprovalCommandResult(ApprovalCommandStatus Status, ApprovalInstance? Instance);

public enum ApprovalCommandStatus
{
    Success,
    NotFound,
    InvalidState,
    Forbidden,
}
