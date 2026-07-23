namespace InnovationToImpact.Domain.EmailTemplates;

public interface IEmailTemplateAttachmentFileStorage
{
    Task<string> SaveAsync(string fileName, byte[] content, CancellationToken cancellationToken = default);
    Task DeleteAsync(string filePath, CancellationToken cancellationToken = default);
}
