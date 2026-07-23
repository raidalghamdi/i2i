using InnovationToImpact.Domain.Auth;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.TrackAssignments;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.TrackAssignments;

public class TrackAssignmentService : ITrackAssignmentService
{
    private readonly InnovationDbContext _db;

    public TrackAssignmentService(InnovationDbContext db)
    {
        _db = db;
    }

    public async Task<TrackAssignmentCommandResult> AssignAsync(Guid evaluatorId, Guid trackId, Guid assignedById, CancellationToken cancellationToken = default)
    {
        var isEvaluator = await _db.Users
            .Where(u => u.Id == evaluatorId)
            .SelectMany(u => u.UserRoles)
            .AnyAsync(ur => ur.Role.Code == RoleCodes.Evaluator, cancellationToken);
        if (!isEvaluator) return new TrackAssignmentCommandResult(TrackAssignmentCommandStatus.InvalidEvaluator);

        var alreadyAssigned = await _db.EvaluatorTrackAssignments
            .AnyAsync(a => a.EvaluatorId == evaluatorId && a.TrackId == trackId, cancellationToken);
        if (alreadyAssigned) return new TrackAssignmentCommandResult(TrackAssignmentCommandStatus.AlreadyAssigned);

        var assignment = new EvaluatorTrackAssignment
        {
            Id = Guid.NewGuid(),
            EvaluatorId = evaluatorId,
            TrackId = trackId,
            AssignedById = assignedById,
        };
        _db.EvaluatorTrackAssignments.Add(assignment);
        await _db.SaveChangesAsync(cancellationToken);

        return new TrackAssignmentCommandResult(TrackAssignmentCommandStatus.Success, assignment);
    }

    public async Task<TrackAssignmentCommandResult> RemoveAsync(Guid assignmentId, CancellationToken cancellationToken = default)
    {
        var assignment = await _db.EvaluatorTrackAssignments.SingleOrDefaultAsync(a => a.Id == assignmentId, cancellationToken);
        if (assignment is null) return new TrackAssignmentCommandResult(TrackAssignmentCommandStatus.NotFound);

        _db.EvaluatorTrackAssignments.Remove(assignment);
        await _db.SaveChangesAsync(cancellationToken);

        return new TrackAssignmentCommandResult(TrackAssignmentCommandStatus.Success);
    }

    public async Task<IReadOnlyList<EvaluatorTrackAssignment>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _db.EvaluatorTrackAssignments
            .Include(a => a.Evaluator)
            .Include(a => a.Track)
            .OrderBy(a => a.Track.NameEn)
            .ThenBy(a => a.Evaluator.FullNameEn)
            .ToListAsync(cancellationToken);
    }
}
