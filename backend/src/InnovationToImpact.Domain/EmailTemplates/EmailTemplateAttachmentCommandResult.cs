using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.EmailTemplates;

public sealed record EmailTemplateAttachmentCommandResult(EmailTemplateAttachmentCommandStatus Status, EmailTemplateAttachment? Entity = default);
