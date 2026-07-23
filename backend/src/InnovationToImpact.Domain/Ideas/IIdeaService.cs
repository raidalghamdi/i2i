using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.Ideas;

public sealed record IdeaListFilter(string? Q, Guid? StrategicThemeId, Guid? ActivityId, string? Status, int? Stage, int Page, int PageSize);
public sealed record IdeaListItem(Guid Id, string Code, string TitleAr, string TitleEn, string ProblemStatementAr, string ProblemStatementEn, int CurrentStage, string Status, Guid StrategicThemeId, Guid? ActivityId);
public sealed record IdeaListPage(IReadOnlyList<IdeaListItem> Items, int Total, int Page, int PageSize);
public sealed record MyIdeaItem(Guid Id, string Code, string TitleAr, string TitleEn, string Status, int CurrentStage, DateTime CreatedAt, DateTime UpdatedAt, int FeedbackCount);

public interface IIdeaService
{
    Task<IdeaQueryResult> CreateAsync(Guid submitterId, IdeaInput input, CancellationToken cancellationToken = default);
    Task<IdeaQueryResult> UpdateAsync(Guid ideaId, Guid submitterId, IdeaInput input, CancellationToken cancellationToken = default);
    Task<IdeaQueryResult> SubmitAsync(Guid ideaId, Guid submitterId, CancellationToken cancellationToken = default);
    Task<IdeaQueryResult> SubmitToCommitteeAsync(Guid ideaId, Guid submitterId, CancellationToken cancellationToken = default);
    Task<IdeaQueryResult> ResubmitAsync(Guid ideaId, Guid submitterId, IdeaResubmitInput input, CancellationToken cancellationToken = default);
    Task<IdeaQueryResult> WithdrawAsync(Guid ideaId, Guid submitterId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MyIdeaItem>> GetMineDetailedAsync(Guid submitterId, string? statusGroup, CancellationToken cancellationToken = default);
    Task<IdeaQueryResult> GetByIdAsync(Guid ideaId, Guid submitterId, bool isElevatedReviewer = false, CancellationToken cancellationToken = default);
    Task<IdeaAttachmentResult> AddAttachmentAsync(Guid ideaId, Guid submitterId, string fileName, string contentType, byte[] content, CancellationToken cancellationToken = default);
    Task<IdeaAttachmentsResult> GetAttachmentsAsync(Guid ideaId, Guid submitterId, bool isElevatedReviewer = false, CancellationToken cancellationToken = default);
    Task<IdeaListPage> ListAsync(IdeaListFilter filter, Guid userId, string userEmail, IReadOnlyCollection<string> roles, CancellationToken cancellationToken = default);
}
