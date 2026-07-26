using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.Ideas;

public sealed record IdeaListFilter(string? Q, Guid? StrategicThemeId, Guid? ActivityId, string? Status, int? Stage, int Page, int PageSize);
public sealed record IdeaListItem(Guid Id, string Code, string TitleAr, string TitleEn, string ProblemStatementAr, string ProblemStatementEn, int CurrentStage, string Status, Guid StrategicThemeId, Guid? ActivityId);
public sealed record IdeaListPage(IReadOnlyList<IdeaListItem> Items, int Total, int Page, int PageSize);
public sealed record MyIdeaItem(Guid Id, string Code, string TitleAr, string TitleEn, string Status, int CurrentStage, DateTime CreatedAt, DateTime UpdatedAt, int FeedbackCount, bool IsOwner, Guid StrategicThemeId, string ThemeNameAr, string ThemeNameEn); // Change 20260726

public interface IIdeaService
{
    Task<IdeaQueryResult> CreateAsync(Guid submitterId, IdeaInput input, CancellationToken cancellationToken = default);
    Task<IdeaQueryResult> UpdateAsync(Guid ideaId, Guid submitterId, IdeaInput input, CancellationToken cancellationToken = default);
    Task<IdeaQueryResult> SubmitAsync(Guid ideaId, Guid submitterId, CancellationToken cancellationToken = default);
    Task<IdeaQueryResult> ResubmitAsync(Guid ideaId, Guid submitterId, IdeaResubmitInput input, CancellationToken cancellationToken = default);
    Task<IdeaQueryResult> WithdrawAsync(Guid ideaId, Guid submitterId, string? reason = null, CancellationToken cancellationToken = default); // Change 20260726
    Task<IReadOnlyList<MyIdeaItem>> GetMineDetailedAsync(Guid userId, string? statusGroup, string userEmail, string? callerSam = null, CancellationToken cancellationToken = default);
    Task<IdeaQueryResult> GetByIdAsync(Guid ideaId, Guid submitterId, bool isElevatedReviewer = false, string? callerSam = null, CancellationToken cancellationToken = default);
    Task<IdeaAttachmentResult> AddAttachmentAsync(Guid ideaId, Guid submitterId, string fileName, string contentType, byte[] content, CancellationToken cancellationToken = default);
    Task<IdeaAttachmentsResult> GetAttachmentsAsync(Guid ideaId, Guid submitterId, bool isElevatedReviewer = false, string? callerSam = null, CancellationToken cancellationToken = default);
    Task<IdeaAttachmentsResult> DeleteAttachmentAsync(Guid ideaId, Guid attachmentId, Guid submitterId, CancellationToken cancellationToken = default); // Change 20260726
    Task<IdeaAttachmentFileResult> GetAttachmentFileAsync(Guid ideaId, Guid attachmentId, Guid userId, bool isElevatedReviewer, string? callerSam, CancellationToken ct = default);
    Task<IdeaListPage> ListAsync(IdeaListFilter filter, Guid userId, string userEmail, IReadOnlyCollection<string> roles, string? callerSam = null, CancellationToken cancellationToken = default);
}
