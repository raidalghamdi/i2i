using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.Assignments;

public interface IAssignmentService
{
    Task<AssignmentPageResult> ListAsync(AssignmentListFilter filter, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkloadRow>> GetWorkloadHeatmapAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SuggestedEvaluator>> SuggestLeastLoadedEvaluatorsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IdeaOption>> ListIdeaOptionsAsync(CancellationToken cancellationToken = default);
    Task<AssignmentCommandResult> CreateAsync(AssignmentCreateInput input, Guid actorId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssignmentCommandResult>> BulkCreateAsync(IReadOnlyList<AssignmentCreateInput> inputs, Guid actorId, CancellationToken cancellationToken = default);
    Task<AssignmentCommandResult> UpdateAsync(Guid id, AssignmentUpdateInput input, Guid actorId, CancellationToken cancellationToken = default);
    Task<AssignmentCommandResult> UnassignAsync(Guid id, Guid actorId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssignmentCommandResult>> BulkUnassignAsync(IReadOnlyList<Guid> ids, Guid actorId, CancellationToken cancellationToken = default);
}
