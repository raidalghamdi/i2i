using InnovationToImpact.Domain.Assignments;
using InnovationToImpact.Domain.Auth;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Reports;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Reports;

// Change 20260726
public class EvaluatorProductivityService : IEvaluatorProductivityService
{
    private readonly InnovationDbContext _db;

    public EvaluatorProductivityService(InnovationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<EvaluatorProductivityRow>> GetAsync(CancellationToken cancellationToken = default)
    {
        // Every evaluator-role user appears, including those with no assignments yet, so the report
        // also surfaces under-utilized evaluators rather than only active ones.
        var evaluators = await _db.Users
            .Where(u => u.UserRoles.Any(ur => ur.Role.Code == RoleCodes.Evaluator))
            .Select(u => new { u.Id, u.FullNameEn, u.FullNameAr, u.SamAccountName })
            .ToListAsync(cancellationToken);

        var evaluatorIds = evaluators.Select(e => e.Id).ToList();

        var assignments = await _db.Set<Assignment>()
            .Where(a => a.Kind == AssignmentKinds.Evaluator && evaluatorIds.Contains(a.EvaluatorId))
            .Select(a => new { a.EvaluatorId, a.IdeaId, a.AssignedAt })
            .ToListAsync(cancellationToken);

        var evaluations = await _db.Evaluations
            .Where(e => evaluatorIds.Contains(e.EvaluatorId))
            .Select(e => new { e.EvaluatorId, e.IdeaId, e.TotalScore, e.SubmittedAt, e.ConflictOfInterest })
            .ToListAsync(cancellationToken);

        // Turnaround is measured from the assignment that produced the evaluation, so it reflects how
        // long the evaluator held the work rather than the idea's total age.
        var assignedAtByPair = assignments
            .GroupBy(a => (a.EvaluatorId, a.IdeaId))
            .ToDictionary(g => g.Key, g => g.Min(a => a.AssignedAt));

        var rows = new List<EvaluatorProductivityRow>();
        foreach (var evaluator in evaluators)
        {
            var mine = evaluations.Where(e => e.EvaluatorId == evaluator.Id).ToList();
            var submitted = mine.Where(e => e.SubmittedAt is not null).ToList();

            // A conflict-of-interest declaration is a recusal, not a score, so it is excluded from the
            // score average while still being counted on its own.
            var scored = submitted.Where(e => !e.ConflictOfInterest).ToList();

            var turnarounds = submitted
                .Where(e => assignedAtByPair.ContainsKey((e.EvaluatorId, e.IdeaId)))
                .Select(e => (e.SubmittedAt!.Value - assignedAtByPair[(e.EvaluatorId, e.IdeaId)]).TotalHours)
                .Where(h => h >= 0)
                .ToList();

            rows.Add(new EvaluatorProductivityRow(
                UserId: evaluator.Id,
                DisplayName: !string.IsNullOrWhiteSpace(evaluator.FullNameEn)
                    ? evaluator.FullNameEn
                    : !string.IsNullOrWhiteSpace(evaluator.FullNameAr)
                        ? evaluator.FullNameAr
                        : evaluator.SamAccountName,
                AssignedCount: assignments.Count(a => a.EvaluatorId == evaluator.Id),
                CompletedCount: submitted.Count,
                DraftCount: mine.Count - submitted.Count,
                AvgScore: scored.Count > 0 ? Math.Round(scored.Average(e => e.TotalScore), 2) : null,
                AvgTurnaroundHours: turnarounds.Count > 0 ? Math.Round(turnarounds.Average(), 2) : null,
                CoiCount: submitted.Count(e => e.ConflictOfInterest)));
        }

        return rows
            .OrderByDescending(r => r.CompletedCount)
            .ThenBy(r => r.DisplayName)
            .ToList();
    }
}
