using InnovationToImpact.Domain.Content;
using InnovationToImpact.Domain.Ideas;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Content;

public class PublicDataService : IPublicDataService
{
    private static readonly string[] PublicIdeaStatuses =
    {
        IdeaStatusCodes.Approved, IdeaStatusCodes.InPilot, IdeaStatusCodes.InMeasurement, IdeaStatusCodes.InScaling,
    };

    private readonly InnovationDbContext _db;
    public PublicDataService(InnovationDbContext db) => _db = db;

    public async Task<IReadOnlyList<PublicTrack>> ListTracksAsync(CancellationToken ct = default) =>
        await _db.StrategicThemes.OrderBy(t => t.Priority)
            .Select(t => new PublicTrack(t.Id, t.NameAr, t.NameEn, t.DescriptionAr, t.DescriptionEn, t.Priority))
            .ToListAsync(ct);

    public async Task<PublicTrackDetail?> GetTrackAsync(Guid id, CancellationToken ct = default)
    {
        var t = await _db.StrategicThemes.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (t is null) return null;
        var challenges = await _db.Challenges.Where(c => c.StrategicThemeId == id && c.IsActive).OrderBy(c => c.SortOrder)
            .Select(c => new PublicChallenge(c.Id, c.TextAr, c.TextEn)).ToListAsync(ct);
        var ideas = await _db.Ideas.Where(i => i.StrategicThemeId == id && PublicIdeaStatuses.Contains(i.IdeaStatus.Code))
            .OrderByDescending(i => i.UpdatedAt).Take(6)
            .Select(i => new PublicIdeaSummary(i.Id, i.Code, i.TitleAr, i.TitleEn, i.IdeaStatus.Code)).ToListAsync(ct);
        return new PublicTrackDetail(new PublicTrack(t.Id, t.NameAr, t.NameEn, t.DescriptionAr, t.DescriptionEn, t.Priority), challenges, ideas);
    }

    public async Task<IReadOnlyList<PublicActivity>> ListActivitiesAsync(CancellationToken ct = default)
    {
        var activities = await _db.Activities.OrderBy(a => a.StartDate).ToListAsync(ct);
        var counts = await _db.Ideas.Where(i => i.ActivityId != null && PublicIdeaStatuses.Contains(i.IdeaStatus.Code))
            .GroupBy(i => i.ActivityId)
            .Select(g => new { ActivityId = g.Key!.Value, Count = g.Count() })
            .ToListAsync(ct);
        var countsByActivity = counts.ToDictionary(c => c.ActivityId, c => c.Count);
        return activities.Select(a => new PublicActivity(a.Id, a.NameAr, a.NameEn, a.Type, a.Status, a.StartDate, a.EndDate,
            countsByActivity.TryGetValue(a.Id, out var count) ? count : 0)).ToList();
    }

    public async Task<PublicActivityDetail?> GetActivityAsync(Guid id, CancellationToken ct = default)
    {
        var a = await _db.Activities.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (a is null) return null;
        // Ordered client-side: SQLite cannot translate ORDER BY over a nullable decimal column.
        var scopedIdeas = await _db.Ideas.Where(i => i.ActivityId == id && PublicIdeaStatuses.Contains(i.IdeaStatus.Code))
            .Select(i => new { i.Id, i.Code, i.TitleAr, i.TitleEn, Status = i.IdeaStatus.Code, i.CommitteeFinalScore })
            .ToListAsync(ct);
        // Totals reflect ALL public ideas for the activity, computed before the list is capped below.
        var approved = scopedIdeas.Count(i => i.Status == IdeaStatusCodes.Approved);
        var piloting = scopedIdeas.Count(i => i.Status == IdeaStatusCodes.InPilot);
        // Cap the returned scoreboard list — this is an anonymous, potentially high-traffic endpoint.
        var scoped = scopedIdeas.OrderByDescending(i => i.CommitteeFinalScore).Take(50)
            .Select(i => new PublicIdeaSummary(i.Id, i.Code, i.TitleAr, i.TitleEn, i.Status)).ToList();
        return new PublicActivityDetail(
            new PublicActivity(a.Id, a.NameAr, a.NameEn, a.Type, a.Status, a.StartDate, a.EndDate, scopedIdeas.Count),
            approved, piloting, scoped);
    }

    public async Task<PublicSearchResults> SearchAsync(string query, CancellationToken ct = default)
    {
        var trimmed = (query ?? string.Empty).Trim();
        if (trimmed.Length == 0)
            return new PublicSearchResults(new List<PublicIdeaSummary>(), new List<PublicTrack>());

        // Escape LIKE metacharacters so a query like "%" matches literally instead of every row.
        var escaped = trimmed.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
        // SQL Server treats '[' as a LIKE character-class opener; escape it so user input matches literally.
        // SQLite (test engine) does not, and only defines ESCAPE for %, _, and the escape char — so scope this to SQL Server.
        if (_db.Database.IsSqlServer())
            escaped = escaped.Replace("[", "\\[");
        var pattern = $"%{escaped}%";

        var ideas = await _db.Ideas
            .Where(i => PublicIdeaStatuses.Contains(i.IdeaStatus.Code) && (
                EF.Functions.Like(i.TitleAr, pattern, "\\") ||
                EF.Functions.Like(i.TitleEn, pattern, "\\") ||
                EF.Functions.Like(i.ProblemStatementAr, pattern, "\\") ||
                EF.Functions.Like(i.ProblemStatementEn, pattern, "\\") ||
                EF.Functions.Like(i.ProposedSolutionAr, pattern, "\\") ||
                EF.Functions.Like(i.ProposedSolutionEn, pattern, "\\")))
            .OrderByDescending(i => i.UpdatedAt)
            .Take(50)
            .Select(i => new PublicIdeaSummary(i.Id, i.Code, i.TitleAr, i.TitleEn, i.IdeaStatus.Code))
            .ToListAsync(ct);

        var tracks = await _db.StrategicThemes
            .Where(t =>
                EF.Functions.Like(t.NameAr, pattern, "\\") ||
                EF.Functions.Like(t.NameEn, pattern, "\\") ||
                (t.DescriptionAr != null && EF.Functions.Like(t.DescriptionAr, pattern, "\\")) ||
                (t.DescriptionEn != null && EF.Functions.Like(t.DescriptionEn, pattern, "\\")))
            .OrderBy(t => t.Priority)
            .Take(20)
            .Select(t => new PublicTrack(t.Id, t.NameAr, t.NameEn, t.DescriptionAr, t.DescriptionEn, t.Priority))
            .ToListAsync(ct);

        return new PublicSearchResults(ideas, tracks);
    }
}
