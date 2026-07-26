namespace InnovationToImpact.Domain.Ideas;

public sealed record IdeaAttachmentFileResult(IdeaCommandStatus Status, byte[]? Content = null, string? ContentType = null, string? FileName = null);
