using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.Ideas;

public sealed record IdeaAttachmentsResult(IdeaCommandStatus Status, IReadOnlyList<EvidenceAttachment> Attachments);
