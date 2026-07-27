using InnovationToImpact.Domain.Approvals;
using InnovationToImpact.Domain.FinalRanking;
using InnovationToImpact.Domain.Ideas;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.FinalRanking;

public class FinalRankingService : IFinalRankingService
{
    private const int DefaultTopN = 5;
    private const string TopNKey = "top_n";

    private readonly InnovationDbContext _db;
    private readonly IApprovalService _approvalService;

    private readonly IIdeaStatusNotifier _statusNotifier; // Change 20260726

    public FinalRankingService(InnovationDbContext db, IApprovalService approvalService, IIdeaStatusNotifier statusNotifier)
    {
        _db = db;
        _approvalService = approvalService;
        _statusNotifier = statusNotifier; // Change 20260726
    }

    public Task<FinalRankingResult> PreviewAsync(CancellationToken cancellationToken = default)
        => ComputeAsync(persist: false, cancellationToken);

    public Task<FinalRankingResult> RunAsync(Guid triggeredById, CancellationToken cancellationToken = default)
        => ComputeAsync(persist: true, cancellationToken);

    private async Task<FinalRankingResult> ComputeAsync(bool persist, CancellationToken cancellationToken)
    {
        var topN = await ReadTopNAsync(cancellationToken);

        var ideas = await _db.Ideas
            .Include(i => i.IdeaStatus)
            .Where(i => i.IdeaStatus.Code == IdeaStatusCodes.PendingFinalRanking)
            .ToListAsync(cancellationToken);

        var approvedStatus = persist ? await _db.IdeaStatuses.SingleAsync(s => s.Code == IdeaStatusCodes.Approved, cancellationToken) : null;
        var notSelectedStatus = persist ? await _db.IdeaStatuses.SingleAsync(s => s.Code == IdeaStatusCodes.NotSelected, cancellationToken) : null;

        var entries = new List<FinalRankingEntry>();
        var approvedCount = 0;
        var notSelectedCount = 0;

        foreach (var group in ideas.GroupBy(i => i.StrategicThemeId))
        {
            var ranked = group
                .OrderByDescending(i => i.CommitteeFinalScore)
                .ThenBy(i => i.CreatedAt)
                .ToList();

            for (var index = 0; index < ranked.Count; index++)
            {
                var idea = ranked[index];
                var rank = index + 1;
                var isApproved = rank <= topN;
                var outcomeStatus = isApproved ? IdeaStatusCodes.Approved : IdeaStatusCodes.NotSelected;

                entries.Add(new FinalRankingEntry(idea.Id, idea.Code, idea.TitleAr, idea.StrategicThemeId, rank, idea.CommitteeFinalScore, outcomeStatus));

                if (isApproved) approvedCount++;
                else notSelectedCount++;

                if (persist)
                {
                    idea.FinalRank = rank;
                    idea.UpdatedAt = DateTime.UtcNow;
                    if (isApproved)
                    {
                        idea.IdeaStatusId = approvedStatus!.Id;
                        idea.IdeaStatus = approvedStatus;
                        idea.ApprovedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        idea.IdeaStatusId = notSelectedStatus!.Id;
                        idea.IdeaStatus = notSelectedStatus;
                    }
                }
            }
        }

        if (persist)
        {
            await _db.SaveChangesAsync(cancellationToken);

            foreach (var idea in ideas.Where(i => i.IdeaStatusId == approvedStatus!.Id))
            {
                await _approvalService.OpenInstanceAsync("idea-approve", "idea", idea.Id, cancellationToken);
            }

            // Change 20260726 — ranking decides both outcomes in one pass, so every ranked idea's
            // submitter is told which way it went.
            foreach (var idea in ideas.Where(i => i.IdeaStatusId == approvedStatus!.Id || i.IdeaStatusId == notSelectedStatus!.Id))
            {
                var outcome = idea.IdeaStatusId == approvedStatus!.Id ? IdeaStatusCodes.Approved : IdeaStatusCodes.NotSelected;
                await _statusNotifier.NotifyStatusChangedAsync(idea, outcome, cancellationToken);
            }
        }

        return new FinalRankingResult(approvedCount, notSelectedCount, topN, entries);
    }

    private async Task<int> ReadTopNAsync(CancellationToken cancellationToken)
    {
        var setting = await _db.AdminSettings.SingleOrDefaultAsync(s => s.Key == TopNKey, cancellationToken);
        if (setting is null) return DefaultTopN;
        return int.TryParse(setting.ValueJson, out var value) ? value : DefaultTopN;
    }
}
