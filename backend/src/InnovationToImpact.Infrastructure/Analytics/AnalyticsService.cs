using InnovationToImpact.Domain.Analytics;
using InnovationToImpact.Domain.Auth;
using InnovationToImpact.Domain.Ideas;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Analytics;

public class AnalyticsService : IAnalyticsService
{
    private static readonly string[] ReachableStatusCodes =
    {
        IdeaStatusCodes.Draft,
        IdeaStatusCodes.Submitted,
        IdeaStatusCodes.Evaluation,
        IdeaStatusCodes.PassAwaitingAttachments,
        IdeaStatusCodes.EvaluationFailed,
        IdeaStatusCodes.Committee,
        IdeaStatusCodes.PendingFinalRanking,
        IdeaStatusCodes.Rejected,
        IdeaStatusCodes.Returned,
        IdeaStatusCodes.Approved,
        IdeaStatusCodes.NotSelected,
    };

    private readonly InnovationDbContext _db;

    public AnalyticsService(InnovationDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Derives an idea's 0..8 pipeline stage from its (authoritative) status code, falling
    /// back to the stored CurrentStage column only when the column has already been advanced
    /// into the post-program range (>= 6), which is the only range production code actually
    /// writes to it (see <see cref="Domain.Ideas.IdeaJourneyCalculator"/> for the on-demand,
    /// fully authoritative computation this approximates for aggregate/reporting queries).
    ///
    /// Scale: 0=draft, 1=submitted/returned, 2=evaluation (incl. evaluation_failed),
    /// 3=pass_awaiting_attachments, 4=committee (incl. rejected), 5=pending_final_ranking/
    /// approved/not_selected, 6=in_pilot, 7=in_measurement, 8=in_scaling.
    /// </summary>
    private static int EffectiveStage(string statusCode, int currentStage)
    {
        if (currentStage >= 6) return Math.Clamp(currentStage, 6, 8);

        return statusCode switch
        {
            IdeaStatusCodes.Draft => 0,
            IdeaStatusCodes.Submitted => 1,
            IdeaStatusCodes.Returned => 1,
            IdeaStatusCodes.Evaluation => 2,
            IdeaStatusCodes.EvaluationFailed => 2,
            IdeaStatusCodes.PassAwaitingAttachments => 3,
            IdeaStatusCodes.Committee => 4,
            IdeaStatusCodes.Rejected => 4,
            IdeaStatusCodes.PendingFinalRanking => 5,
            IdeaStatusCodes.Approved => 5,
            IdeaStatusCodes.NotSelected => 5,
            IdeaStatusCodes.InPilot => 6,
            IdeaStatusCodes.InMeasurement => 7,
            IdeaStatusCodes.InScaling => 8,
            IdeaStatusCodes.Withdrawn => 2,
            _ => Math.Clamp(currentStage, 0, 8),
        };
    }

    private static readonly string[] EvaluatedOrBeyondStatusCodes =
    {
        IdeaStatusCodes.Evaluation,
        IdeaStatusCodes.PassAwaitingAttachments,
        IdeaStatusCodes.Committee,
        IdeaStatusCodes.PendingFinalRanking,
        IdeaStatusCodes.Approved,
        IdeaStatusCodes.InPilot,
        IdeaStatusCodes.InMeasurement,
        IdeaStatusCodes.InScaling,
    };

    public async Task<PlatformKpis> GetPlatformKpisAsync(CancellationToken cancellationToken = default)
    {
        var totalIdeas = await _db.Ideas.CountAsync(cancellationToken);
        var totalApproved = await _db.Ideas.CountAsync(i => i.IdeaStatus.Code == IdeaStatusCodes.Approved, cancellationToken);
        var totalSubmitters = await _db.Ideas.Select(i => i.SubmitterId).Distinct().CountAsync(cancellationToken);
        var totalEvaluations = await _db.Evaluations.CountAsync(cancellationToken);
        var totalEvaluators = await _db.Users.CountAsync(u => u.UserRoles.Any(ur => ur.Role.Code == RoleCodes.Evaluator), cancellationToken);

        return new PlatformKpis(totalIdeas, totalApproved, totalSubmitters, totalEvaluations, totalEvaluators);
    }

    public async Task<IReadOnlyList<IdeasByStatusEntry>> GetIdeasByStatusAsync(CancellationToken cancellationToken = default)
    {
        var statuses = await _db.IdeaStatuses
            .Where(s => ReachableStatusCodes.Contains(s.Code))
            .OrderBy(s => s.SortOrder)
            .ToListAsync(cancellationToken);

        var counts = await _db.Ideas
            .GroupBy(i => i.IdeaStatusId)
            .Select(g => new { StatusId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return statuses
            .Select(s => new IdeasByStatusEntry(s.Code, s.NameEn, counts.SingleOrDefault(c => c.StatusId == s.Id)?.Count ?? 0))
            .ToList();
    }

    public async Task<IReadOnlyList<SubmissionsOverTimeEntry>> GetSubmissionsOverTimeAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-90);
        var createdDates = await _db.Ideas
            .Where(i => i.CreatedAt >= cutoff)
            .Select(i => i.CreatedAt)
            .ToListAsync(cancellationToken);

        return createdDates
            .GroupBy(d => d.Date)
            .Select(g => new SubmissionsOverTimeEntry(g.Key, g.Count()))
            .OrderBy(e => e.Date)
            .ToList();
    }

    public async Task<IReadOnlyList<ThemeActivityEntry>> GetThemeActivityAsync(CancellationToken cancellationToken = default)
    {
        var themes = await _db.StrategicThemes.ToListAsync(cancellationToken);
        var ideas = await _db.Ideas
            .Select(i => new { i.StrategicThemeId, StatusCode = i.IdeaStatus.Code })
            .ToListAsync(cancellationToken);

        return themes
            .Select(t =>
            {
                var themeIdeas = ideas.Where(i => i.StrategicThemeId == t.Id).ToList();
                return new ThemeActivityEntry(t.NameEn, themeIdeas.Count, themeIdeas.Count(i => i.StatusCode == IdeaStatusCodes.Approved));
            })
            .OrderByDescending(e => e.IdeaCount)
            .ToList();
    }
    public async Task<IReadOnlyList<TopEvaluatorEntry>> GetTopEvaluatorsAsync(CancellationToken cancellationToken = default)
    {
        var evaluations = await _db.Evaluations
            .Select(e => new { e.EvaluatorId, e.TotalScore })
            .ToListAsync(cancellationToken);

        var evaluatorIds = evaluations.Select(e => e.EvaluatorId).Distinct().ToList();
        var evaluatorNames = await _db.Users
            .Where(u => evaluatorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullNameEn, cancellationToken);

        return evaluations
            .GroupBy(e => e.EvaluatorId)
            .Select(g => new TopEvaluatorEntry(
                evaluatorNames.GetValueOrDefault(g.Key, "Unknown"),
                g.Count(),
                g.Average(e => e.TotalScore)))
            .OrderByDescending(e => e.EvaluationCount)
            .Take(10)
            .ToList();
    }

    public async Task<SlaComplianceResult> GetSlaComplianceAsync(CancellationToken cancellationToken = default)
    {
        var total = await _db.SlaTrackings.CountAsync(cancellationToken);
        if (total == 0) return new SlaComplianceResult(null, 0);

        var compliant = await _db.SlaTrackings.CountAsync(t => t.BreachedAt == null, cancellationToken);
        return new SlaComplianceResult((double)compliant / total * 100, total);
    }

    private static readonly string[] CohortApprovedStatusCodes =
    {
        IdeaStatusCodes.Approved,
        IdeaStatusCodes.InPilot,
        IdeaStatusCodes.InMeasurement,
        IdeaStatusCodes.InScaling,
    };

    private static readonly string[] CohortRejectedStatusCodes =
    {
        IdeaStatusCodes.Rejected,
        IdeaStatusCodes.EvaluationFailed,
        IdeaStatusCodes.NotSelected,
    };

    private static readonly string[] CohortImplementedStatusCodes =
    {
        IdeaStatusCodes.InMeasurement,
        IdeaStatusCodes.InScaling,
    };

    private static readonly string[] PilotStatusCodes =
    {
        IdeaStatusCodes.InPilot,
        IdeaStatusCodes.InMeasurement,
        IdeaStatusCodes.InScaling,
    };

    public async Task<IReadOnlyList<FunnelEntry>> GetFunnelAsync(CancellationToken cancellationToken = default)
    {
        var ideas = await _db.Ideas
            .Select(i => new { StatusCode = i.IdeaStatus.Code, i.CurrentStage })
            .ToListAsync(cancellationToken);

        var participation = ideas.Count(i => i.StatusCode != IdeaStatusCodes.Draft);
        var evaluated = ideas.Count(i => EvaluatedOrBeyondStatusCodes.Contains(i.StatusCode) || i.CurrentStage >= 6);
        var approved = ideas.Count(i => i.StatusCode == IdeaStatusCodes.Approved);
        var piloted = ideas.Count(i => PilotStatusCodes.Contains(i.StatusCode) || i.CurrentStage >= 6);
        var scaled = ideas.Count(i => i.StatusCode == IdeaStatusCodes.InScaling || i.CurrentStage >= 8);

        return new List<FunnelEntry>
        {
            new("Participation", participation),
            new("Evaluated", evaluated),
            new("Approved", approved),
            new("Piloted", piloted),
            new("Scaled", scaled),
        };
    }

    public async Task<IReadOnlyList<CohortEntry>> GetCohortAsync(CancellationToken cancellationToken = default)
    {
        var ideas = await _db.Ideas
            .Select(i => new { StatusCode = i.IdeaStatus.Code, i.CreatedAt })
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var months = Enumerable.Range(0, 12)
            .Select(offset => new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-(11 - offset)))
            .ToList();

        return months
            .Select(monthStart =>
            {
                var monthKey = monthStart.ToString("yyyy-MM");
                var inMonth = ideas.Where(i => i.CreatedAt.Year == monthStart.Year && i.CreatedAt.Month == monthStart.Month).ToList();

                return new CohortEntry(
                    monthKey,
                    inMonth.Count,
                    inMonth.Count(i => CohortApprovedStatusCodes.Contains(i.StatusCode)),
                    inMonth.Count(i => CohortRejectedStatusCodes.Contains(i.StatusCode)),
                    inMonth.Count(i => CohortImplementedStatusCodes.Contains(i.StatusCode)));
            })
            .ToList();
    }

    public async Task<IReadOnlyList<IdeasByStageEntry>> GetIdeasByStageAsync(CancellationToken cancellationToken = default)
    {
        var ideas = await _db.Ideas
            .Select(i => new { StatusCode = i.IdeaStatus.Code, i.CurrentStage })
            .ToListAsync(cancellationToken);

        var effectiveStages = ideas.Select(i => EffectiveStage(i.StatusCode, i.CurrentStage)).ToList();

        return Enumerable.Range(0, 9)
            .Select(stage => new IdeasByStageEntry(stage, effectiveStages.Count(s => s == stage)))
            .ToList();
    }

    public async Task<IReadOnlyList<SubmissionsOverTimeEntry>> GetSubmissionsOverTimeFilledAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var cutoff = today.AddDays(-89);

        var createdDates = await _db.Ideas
            .Where(i => i.CreatedAt >= cutoff && i.IdeaStatus.Code != IdeaStatusCodes.Draft)
            .Select(i => i.CreatedAt)
            .ToListAsync(cancellationToken);

        var counts = createdDates
            .GroupBy(d => d.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        return Enumerable.Range(0, 90)
            .Select(offset => cutoff.AddDays(offset))
            .Select(date => new SubmissionsOverTimeEntry(date, counts.GetValueOrDefault(date, 0)))
            .ToList();
    }

    public async Task<IReadOnlyList<TopObjectiveEntry>> GetTopObjectivesAsync(CancellationToken cancellationToken = default)
    {
        var themes = await _db.StrategicThemes.ToDictionaryAsync(t => t.Id, cancellationToken);

        var counts = await _db.Ideas
            .GroupBy(i => i.StrategicThemeId)
            .Select(g => new { ThemeId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return counts
            .OrderByDescending(c => c.Count)
            .Take(5)
            .Select(c => themes.TryGetValue(c.ThemeId, out var theme)
                ? new TopObjectiveEntry(c.ThemeId, theme.NameAr, theme.NameEn, c.Count)
                : new TopObjectiveEntry(c.ThemeId, string.Empty, string.Empty, c.Count))
            .ToList();
    }

    public async Task<IReadOnlyList<AvgTimePerStageEntry>> GetAvgTimePerStageAsync(CancellationToken cancellationToken = default)
    {
        var ideas = await _db.Ideas
            .Select(i => new { StatusCode = i.IdeaStatus.Code, i.CurrentStage, i.CreatedAt, i.UpdatedAt })
            .ToListAsync(cancellationToken);

        var buckets = new Dictionary<int, List<double>>();
        foreach (var idea in ideas)
        {
            var effectiveStage = EffectiveStage(idea.StatusCode, idea.CurrentStage);
            if (effectiveStage <= 0) continue;

            var perStage = (idea.UpdatedAt - idea.CreatedAt).TotalDays / effectiveStage;
            var maxStage = Math.Min(effectiveStage, 8);
            for (var s = 1; s <= maxStage; s++)
            {
                if (!buckets.TryGetValue(s, out var list))
                {
                    list = new List<double>();
                    buckets[s] = list;
                }
                list.Add(perStage);
            }
        }

        return buckets
            .OrderBy(b => b.Key)
            .Select(b => new AvgTimePerStageEntry(b.Key, Math.Round(b.Value.Average(), 1)))
            .ToList();
    }

    public async Task<ConversionResult> GetConversionAsync(CancellationToken cancellationToken = default)
    {
        var ideas = await _db.Ideas
            .Select(i => new { StatusCode = i.IdeaStatus.Code, i.CurrentStage })
            .ToListAsync(cancellationToken);

        var submitted = ideas.Count(i => i.StatusCode != IdeaStatusCodes.Draft);
        var pilot = ideas.Count(i => i.CurrentStage >= 6 || PilotStatusCodes.Contains(i.StatusCode));
        var rate = Math.Round((double)pilot / Math.Max(submitted, 1) * 100, 1);

        return new ConversionResult(submitted, pilot, rate);
    }

    public async Task<ExtendedPlatformKpis> GetExtendedPlatformKpisAsync(CancellationToken cancellationToken = default)
    {
        var ideas = await _db.Ideas
            .Select(i => new { StatusCode = i.IdeaStatus.Code, i.SubmitterId })
            .ToListAsync(cancellationToken);

        var totalSubmissions = ideas.Count(i => i.StatusCode != IdeaStatusCodes.Draft);
        var totalApproved = ideas.Count(i => i.StatusCode == IdeaStatusCodes.Approved);
        var totalImplemented = ideas.Count(i => i.StatusCode == IdeaStatusCodes.InMeasurement || i.StatusCode == IdeaStatusCodes.InScaling);
        var activeSubmitters = ideas.Select(i => i.SubmitterId).Distinct().Count();

        var totalEvaluations = await _db.Evaluations.CountAsync(cancellationToken);
        var totalUsers = await _db.Users.CountAsync(cancellationToken);
        var totalEvaluators = await _db.Users.CountAsync(u => u.UserRoles.Any(ur => ur.Role.Code == RoleCodes.Evaluator), cancellationToken);

        var financialRealizedValues = await _db.Benefits
            .Where(b => b.BenefitCategory.Code == "financial")
            .Select(b => b.RealizedValue)
            .ToListAsync(cancellationToken);
        var realizedFinancialImpact = financialRealizedValues.Sum(v => v ?? 0m);

        return new ExtendedPlatformKpis(
            totalSubmissions,
            totalApproved,
            totalImplemented,
            activeSubmitters,
            totalEvaluations,
            totalUsers,
            totalEvaluators,
            realizedFinancialImpact);
    }

    private static readonly string[] PillarTerminalStatusCodes =
    {
        IdeaStatusCodes.Rejected,
        IdeaStatusCodes.EvaluationFailed,
        IdeaStatusCodes.NotSelected,
        IdeaStatusCodes.Withdrawn,
    };

    private static readonly string[] PillarImplementedStatusCodes =
    {
        IdeaStatusCodes.InMeasurement,
        IdeaStatusCodes.InScaling,
    };

    public async Task<PillarDetail?> GetPillarDetailAsync(Guid themeId, CancellationToken cancellationToken = default)
    {
        var theme = await _db.StrategicThemes
            .Include(t => t.Owner)
            .SingleOrDefaultAsync(t => t.Id == themeId, cancellationToken);

        if (theme is null) return null;

        var ideas = await _db.Ideas
            .Where(i => i.StrategicThemeId == themeId)
            .Select(i => new
            {
                i.Id,
                i.Code,
                i.TitleAr,
                i.TitleEn,
                StatusCode = i.IdeaStatus.Code,
                i.CurrentStage,
                i.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        var ideaIds = ideas.Select(i => i.Id).ToList();

        var financialValues = await _db.Benefits
            .Where(b => b.BenefitCategory.Code == "financial" && ideaIds.Contains(b.IdeaId))
            .Select(b => new { b.RealizedValue, b.TargetValue })
            .ToListAsync(cancellationToken);

        var budgetSpent = financialValues.Sum(v => v.RealizedValue ?? 0m);
        var budgetAllocated = financialValues.Sum(v => v.TargetValue ?? 0m);

        var pilotsActive = ideas.Count(i => i.CurrentStage >= 6 && !PillarTerminalStatusCodes.Contains(i.StatusCode));
        var implementationsDone = ideas.Count(i => PillarImplementedStatusCodes.Contains(i.StatusCode));

        var kpis = new PillarKpis(ideas.Count, budgetSpent, budgetAllocated, pilotsActive, implementationsDone);

        var now = DateTime.UtcNow;
        var months = Enumerable.Range(0, 12)
            .Select(offset => new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-(11 - offset)))
            .ToList();

        var timeline = months
            .Select(monthStart => new PillarTimelineEntry(
                monthStart.ToString("yyyy-MM"),
                ideas.Count(i => i.CreatedAt.Year == monthStart.Year && i.CreatedAt.Month == monthStart.Month)))
            .ToList();

        var ideaRows = ideas
            .OrderByDescending(i => i.CurrentStage)
            .Select(i => new PillarIdeaRow(i.Id, i.Code, i.TitleAr, i.TitleEn, i.StatusCode, i.CurrentStage))
            .ToList();

        return new PillarDetail(
            theme.Id,
            theme.NameAr,
            theme.NameEn,
            theme.DescriptionAr,
            theme.DescriptionEn,
            theme.Owner?.FullNameEn,
            kpis,
            timeline,
            ideaRows);
    }
}
