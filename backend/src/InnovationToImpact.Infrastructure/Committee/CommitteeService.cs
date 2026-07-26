using System.Text.Json;
using InnovationToImpact.Domain.Approvals;
using InnovationToImpact.Domain.Assignments;
using InnovationToImpact.Domain.Committee;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Ideas;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Committee;

public class CommitteeService : ICommitteeService
{
    private readonly InnovationDbContext _db;
    private readonly IApprovalService _approvalService;

    public CommitteeService(InnovationDbContext db, IApprovalService approvalService)
    {
        _db = db;
        _approvalService = approvalService;
    }

    public async Task<CommitteeCommandResult> SubmitDecisionAsync(Guid ideaId, Guid judgeId, CommitteeDecisionInput input, CancellationToken cancellationToken = default)
    {
        var idea = await _db.Ideas.Include(i => i.IdeaStatus).SingleOrDefaultAsync(i => i.Id == ideaId, cancellationToken);
        if (idea is null) return new CommitteeCommandResult(CommitteeCommandStatus.NotFound);
        if (idea.IdeaStatus.Code != IdeaStatusCodes.Committee) return new CommitteeCommandResult(CommitteeCommandStatus.InvalidState);

        // Quorum integrity: only a judge with a per-idea judge Assignment may decide on this idea.
        // Without this gate any judge (or, before this fix, any supervisor) could submit a decision,
        // inflating the decision count against the assigned-judge quorum below.
        var isAssignedJudge = await _db.Set<Assignment>().AnyAsync(
            a => a.IdeaId == ideaId && a.EvaluatorId == judgeId && a.Kind == AssignmentKinds.Judge,
            cancellationToken);
        if (!isAssignedJudge) return new CommitteeCommandResult(CommitteeCommandStatus.Forbidden);

        var alreadyDecided = await _db.CommitteeDecisions.AnyAsync(d => d.IdeaId == ideaId && d.DecidedById == judgeId, cancellationToken);
        if (alreadyDecided) return new CommitteeCommandResult(CommitteeCommandStatus.AlreadyDecided);

        var decisionType = await _db.CommitteeDecisionTypes.SingleOrDefaultAsync(t => t.Code == input.DecisionTypeCode, cancellationToken);
        if (decisionType is null) return new CommitteeCommandResult(CommitteeCommandStatus.InvalidDecisionType);

        var activeCriteria = await _db.CommitteeCriteria.Where(c => c.Active).ToListAsync(cancellationToken);
        var activeCodes = activeCriteria.Select(c => c.Code).ToHashSet();
        var inputCodes = input.CriteriaScores.Keys.ToHashSet();
        if (!activeCodes.SetEquals(inputCodes)) return new CommitteeCommandResult(CommitteeCommandStatus.InvalidCriteria);
        if (input.CriteriaScores.Values.Any(s => s < 0 || s > 10)) return new CommitteeCommandResult(CommitteeCommandStatus.InvalidCriteria);

        var totalScore = activeCriteria.Sum(c => input.CriteriaScores[c.Code] * c.Weight);

        var decision = new CommitteeDecision
        {
            Id = Guid.NewGuid(),
            IdeaId = ideaId,
            CommitteeName = "Committee",
            CommitteeDecisionTypeId = decisionType.Id,
            DecidedById = judgeId,
            DecidedAt = DateTime.UtcNow,
            Comments = input.Comments,
            CriteriaScoresJson = JsonSerializer.Serialize(input.CriteriaScores),
            TotalScore = totalScore,
        };
        _db.CommitteeDecisions.Add(decision);
        await _db.SaveChangesAsync(cancellationToken);

        // Quorum denominator: the idea's ASSIGNED judges, not every judge-role user in the system.
        // Combined with the assigned-judge gate above (only assigned judges can create a decision),
        // decisionCount is guaranteed to be a subset of assignedJudgeCount.
        var assignedJudgeCount = await _db.Set<Assignment>().CountAsync(a => a.IdeaId == ideaId && a.Kind == AssignmentKinds.Judge, cancellationToken);
        var decisionCount = await _db.CommitteeDecisions.CountAsync(d => d.IdeaId == ideaId, cancellationToken);

        if (decisionCount >= assignedJudgeCount)
        {
            var allScores = await _db.CommitteeDecisions.Where(d => d.IdeaId == ideaId).Select(d => d.TotalScore).ToListAsync(cancellationToken);
            idea.CommitteeFinalScore = allScores.Average();

            var rankingStatus = await _db.IdeaStatuses.SingleAsync(s => s.Code == IdeaStatusCodes.PendingFinalRanking, cancellationToken);
            idea.IdeaStatusId = rankingStatus.Id;
            idea.IdeaStatus = rankingStatus;
            idea.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            // Entity id choice: the committee-publish gate is opened against the IDEA id (not the
            // CommitteeDecision id) so the approvals queue card can resolve a title/code for display
            // via the same idea lookup used elsewhere in the queue, and so a single gate instance
            // covers "publish this idea's committee outcome" regardless of how many judge decisions
            // fed into it.
            await _approvalService.OpenInstanceAsync("committee-publish", "committee_decision", idea.Id, cancellationToken);
        }

        return new CommitteeCommandResult(CommitteeCommandStatus.Success, decision, idea);
    }

    public async Task<IReadOnlyList<CommitteeQueueItem>> GetQueueAsync(Guid judgeId, CancellationToken cancellationToken = default)
    {
        // Judges may view all committee-status ideas, but each item's totalJudges reflects that
        // idea's own ASSIGNED judges (per-idea Assignment count), not the global judge-role headcount.
        var ideas = await _db.Ideas
            .Include(i => i.IdeaStatus)
            .Include(i => i.Submitter)
            .Where(i => i.IdeaStatus.Code == IdeaStatusCodes.Committee)
            .OrderBy(i => i.UpdatedAt)
            .ToListAsync(cancellationToken);

        var result = new List<CommitteeQueueItem>();
        foreach (var idea in ideas)
        {
            var assignedJudgeCount = await _db.Set<Assignment>().CountAsync(a => a.IdeaId == idea.Id && a.Kind == AssignmentKinds.Judge, cancellationToken);
            var decidedCount = await _db.CommitteeDecisions.CountAsync(d => d.IdeaId == idea.Id, cancellationToken);
            var hasDecided = await _db.CommitteeDecisions.AnyAsync(d => d.IdeaId == idea.Id && d.DecidedById == judgeId, cancellationToken);
            result.Add(new CommitteeQueueItem(idea, hasDecided, decidedCount, assignedJudgeCount));
        }
        return result;
    }

    public async Task<IReadOnlyList<CommitteeDecision>> GetMyDecisionsAsync(Guid judgeId, CancellationToken cancellationToken = default)
    {
        return await _db.CommitteeDecisions
            .Include(d => d.Idea)
            .Where(d => d.DecidedById == judgeId)
            .OrderByDescending(d => d.DecidedAt)
            .ToListAsync(cancellationToken);
    }
}
