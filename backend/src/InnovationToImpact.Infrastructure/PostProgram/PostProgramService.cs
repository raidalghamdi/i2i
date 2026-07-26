using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Ideas;
using InnovationToImpact.Domain.PostProgram;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.PostProgram;

public class PostProgramService : IPostProgramService
{
    private static readonly IReadOnlyDictionary<string, string> NextStage = new Dictionary<string, string>
    {
        [IdeaStatusCodes.Approved] = IdeaStatusCodes.InPilot,
        [IdeaStatusCodes.InPilot] = IdeaStatusCodes.InMeasurement,
        [IdeaStatusCodes.InMeasurement] = IdeaStatusCodes.InScaling,
    };

    private static readonly IReadOnlyDictionary<string, int> StageNumber = new Dictionary<string, int>
    {
        [IdeaStatusCodes.InPilot] = 6,
        [IdeaStatusCodes.InMeasurement] = 7,
        [IdeaStatusCodes.InScaling] = 8,
    };

    private static readonly string[] PostProgramCodes =
    {
        IdeaStatusCodes.Approved, IdeaStatusCodes.InPilot, IdeaStatusCodes.InMeasurement, IdeaStatusCodes.InScaling,
    };

    private readonly InnovationDbContext _db;

    public PostProgramService(InnovationDbContext db)
    {
        _db = db;
    }

    public async Task<PostProgramAdvanceResult> AdvanceAsync(Guid ideaId, string? targetStage, Guid actorId, string? comment, CancellationToken cancellationToken = default)
    {
        if (targetStage is not (IdeaStatusCodes.InPilot or IdeaStatusCodes.InMeasurement or IdeaStatusCodes.InScaling))
            return new PostProgramAdvanceResult(PostProgramAdvanceStatus.InvalidStage);

        var idea = await _db.Ideas.Include(i => i.IdeaStatus).SingleOrDefaultAsync(i => i.Id == ideaId, cancellationToken);
        if (idea is null) return new PostProgramAdvanceResult(PostProgramAdvanceStatus.NotFound);

        if (!NextStage.TryGetValue(idea.IdeaStatus.Code, out var expected) || expected != targetStage)
            return new PostProgramAdvanceResult(PostProgramAdvanceStatus.InvalidTransition);

        if (string.IsNullOrWhiteSpace(comment))
            return new PostProgramAdvanceResult(PostProgramAdvanceStatus.CommentRequired);

        var fromStage = idea.IdeaStatus.Code;
        var nextStatus = await _db.IdeaStatuses.SingleAsync(s => s.Code == targetStage, cancellationToken);
        idea.IdeaStatusId = nextStatus.Id;
        idea.IdeaStatus = nextStatus;
        idea.CurrentStage = StageNumber[targetStage];
        idea.UpdatedAt = DateTime.UtcNow;

        _db.PostProgramHistories.Add(new PostProgramHistory
        {
            Id = Guid.NewGuid(),
            IdeaId = idea.Id,
            FromStage = fromStage,
            ToStage = targetStage,
            Comment = comment!,
            ChangedById = actorId,
            ChangedAt = DateTime.UtcNow,
        });

        await _db.SaveChangesAsync(cancellationToken);

        return new PostProgramAdvanceResult(PostProgramAdvanceStatus.Success, idea);
    }

    public async Task<IReadOnlyList<Idea>> GetPostProgramIdeasAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Ideas
            .Include(i => i.IdeaStatus)
            .Where(i => PostProgramCodes.Contains(i.IdeaStatus.Code))
            .OrderBy(i => i.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PostProgramHistory>> GetHistoryAsync(Guid ideaId, CancellationToken cancellationToken = default)
    {
        return await _db.PostProgramHistories
            .Where(h => h.IdeaId == ideaId)
            .OrderBy(h => h.ChangedAt)
            .ToListAsync(cancellationToken);
    }
}
