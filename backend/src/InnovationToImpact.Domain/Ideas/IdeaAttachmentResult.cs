using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.Ideas;

public sealed record IdeaAttachmentResult(IdeaCommandStatus Status, EvidenceAttachment? Attachment = null);
