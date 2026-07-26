using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.EmailTemplates;

public interface IEmailTemplateAttachmentService
{
    Task<IReadOnlyList<EmailTemplateAttachment>> ListByTemplateAsync(Guid templateId, CancellationToken cancellationToken = default);
    Task<EmailTemplateAttachmentCommandResult> UploadAsync(Guid templateId, string fileName, string contentType, byte[] content, Guid actorId, CancellationToken cancellationToken = default);
    Task<EmailTemplateAttachmentCommandResult> DeleteAsync(Guid id, Guid actorId, CancellationToken cancellationToken = default);
}
