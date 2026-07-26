using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.PostProgram;

public enum PostProgramAdvanceStatus
{
    Success,
    NotFound,
    InvalidStage,
    InvalidTransition,
    CommentRequired,
}

public sealed record PostProgramAdvanceResult(PostProgramAdvanceStatus Status, Idea? Idea = null);

public sealed record PostProgramStageInput(string? Stage, string? Comment);

public interface IPostProgramService
{
    Task<PostProgramAdvanceResult> AdvanceAsync(Guid ideaId, string? targetStage, Guid actorId, string? comment, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Idea>> GetPostProgramIdeasAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PostProgramHistory>> GetHistoryAsync(Guid ideaId, CancellationToken cancellationToken = default);
}
