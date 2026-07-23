using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.PostProgram;

public enum PostProgramAdvanceStatus
{
    Success,
    NotFound,
    InvalidStage,
    InvalidTransition,
}

public sealed record PostProgramAdvanceResult(PostProgramAdvanceStatus Status, Idea? Idea = null);

public sealed record PostProgramStageInput(string? Stage);

public interface IPostProgramService
{
    Task<PostProgramAdvanceResult> AdvanceAsync(Guid ideaId, string? targetStage, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Idea>> GetPostProgramIdeasAsync(CancellationToken cancellationToken = default);
}
