using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.EmailTemplates;

public interface IEmailTemplateService
{
    Task<IReadOnlyList<EmailTemplate>> ListAsync(CancellationToken cancellationToken = default);
    Task<EmailTemplateCommandResult> UpdateAsync(Guid id, EmailTemplateInput input, Guid actorId, CancellationToken cancellationToken = default);
}
