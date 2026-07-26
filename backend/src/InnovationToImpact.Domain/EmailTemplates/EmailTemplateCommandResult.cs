using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.EmailTemplates;

public sealed record EmailTemplateCommandResult(EmailTemplateCommandStatus Status, EmailTemplate? Entity = default);
