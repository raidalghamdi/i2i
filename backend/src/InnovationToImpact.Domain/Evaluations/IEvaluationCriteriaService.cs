using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.Evaluations;

// Change 20260726
public sealed record EvaluationCriterionInput(
    string Code,
    string NameAr,
    string NameEn,
    string? DescriptionAr,
    string? DescriptionEn,
    decimal Weight,
    bool Active,
    int SortOrder);

// Change 20260726
public enum EvaluationCriteriaCommandStatus
{
    Success,
    NotFound,
    DuplicateCode,
}

// Change 20260726
public sealed record EvaluationCriteriaCommandResult(EvaluationCriteriaCommandStatus Status, EvaluationCriterion? Entity);

// Change 20260726
public interface IEvaluationCriteriaService
{
    Task<IReadOnlyList<EvaluationCriterion>> ListActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EvaluationCriterion>> ListAllAsync(CancellationToken cancellationToken = default);
    Task<EvaluationCriteriaCommandResult> CreateAsync(EvaluationCriterionInput input, Guid actorId, CancellationToken cancellationToken = default);
    Task<EvaluationCriteriaCommandResult> UpdateAsync(Guid id, EvaluationCriterionInput input, Guid actorId, CancellationToken cancellationToken = default);
    Task<EvaluationCriteriaCommandResult> DeleteAsync(Guid id, Guid actorId, CancellationToken cancellationToken = default);
}
