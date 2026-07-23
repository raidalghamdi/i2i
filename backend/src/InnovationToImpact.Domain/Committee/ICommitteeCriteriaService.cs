using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.Committee;

public sealed record CommitteeCriterionInput(
    string Code,
    string NameAr,
    string NameEn,
    string? DescriptionAr,
    string? DescriptionEn,
    decimal Weight,
    bool Active);

public enum CommitteeCriteriaCommandStatus
{
    Success,
    NotFound,
    DuplicateCode,
}

public sealed record CommitteeCriteriaCommandResult(CommitteeCriteriaCommandStatus Status, CommitteeCriterion? Entity);

public interface ICommitteeCriteriaService
{
    Task<IReadOnlyList<CommitteeCriterion>> ListAllAsync(CancellationToken cancellationToken = default);
    Task<CommitteeCriteriaCommandResult> CreateAsync(CommitteeCriterionInput input, Guid actorId, CancellationToken cancellationToken = default);
    Task<CommitteeCriteriaCommandResult> UpdateAsync(Guid id, CommitteeCriterionInput input, Guid actorId, CancellationToken cancellationToken = default);
    Task<CommitteeCriteriaCommandResult> DeleteAsync(Guid id, Guid actorId, CancellationToken cancellationToken = default);
}
