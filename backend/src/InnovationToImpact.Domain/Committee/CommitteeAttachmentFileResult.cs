namespace InnovationToImpact.Domain.Committee;

// Change 20260726
public sealed record CommitteeAttachmentFileResult(
    CommitteeCommandStatus Status,
    byte[]? Content = null,
    string? ContentType = null,
    string? FileName = null);
