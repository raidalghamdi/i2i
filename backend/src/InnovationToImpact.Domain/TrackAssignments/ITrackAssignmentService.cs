using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.TrackAssignments;

public interface ITrackAssignmentService
{
    Task<TrackAssignmentCommandResult> AssignAsync(Guid evaluatorId, Guid trackId, Guid assignedById, CancellationToken cancellationToken = default);
    Task<TrackAssignmentCommandResult> RemoveAsync(Guid assignmentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EvaluatorTrackAssignment>> ListAsync(CancellationToken cancellationToken = default);
}
