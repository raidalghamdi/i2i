using System.Text.Json;
using InnovationToImpact.Domain.Audit;
using InnovationToImpact.Domain.EmailTemplates;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.EmailTemplates;

public class EmailTemplateAttachmentService : IEmailTemplateAttachmentService
{
    private readonly InnovationDbContext _db;
    private readonly IEmailTemplateAttachmentFileStorage _storage;
    private readonly IAuditLogWriter _auditLogWriter;

    public EmailTemplateAttachmentService(InnovationDbContext db, IEmailTemplateAttachmentFileStorage storage, IAuditLogWriter auditLogWriter)
    {
        _db = db;
        _storage = storage;
        _auditLogWriter = auditLogWriter;
    }

    public async Task<IReadOnlyList<EmailTemplateAttachment>> ListByTemplateAsync(Guid templateId, CancellationToken cancellationToken = default) =>
        await _db.EmailTemplateAttachments.Where(a => a.EmailTemplateId == templateId).OrderBy(a => a.UploadedAt).ToListAsync(cancellationToken);

    public async Task<EmailTemplateAttachmentCommandResult> UploadAsync(Guid templateId, string fileName, string contentType, byte[] content, Guid actorId, CancellationToken cancellationToken = default)
    {
        var templateExists = await _db.EmailTemplates.AnyAsync(t => t.Id == templateId, cancellationToken);
        if (!templateExists) return new EmailTemplateAttachmentCommandResult(EmailTemplateAttachmentCommandStatus.TemplateNotFound);

        var blobPath = await _storage.SaveAsync(fileName, content, cancellationToken);
        var attachment = new EmailTemplateAttachment
        {
            Id = Guid.NewGuid(),
            EmailTemplateId = templateId,
            UploaderId = actorId,
            FileName = fileName,
            BlobPath = blobPath,
            ContentType = contentType,
            FileSizeBytes = content.Length,
        };
        _db.EmailTemplateAttachments.Add(attachment);
        await _db.SaveChangesAsync(cancellationToken);
        await _auditLogWriter.AppendAsync("email_template_attachment", attachment.Id, "upload", actorId, JsonSerializer.Serialize(new { fileName, templateId }), cancellationToken);
        return new EmailTemplateAttachmentCommandResult(EmailTemplateAttachmentCommandStatus.Success, attachment);
    }

    public async Task<EmailTemplateAttachmentCommandResult> DeleteAsync(Guid id, Guid actorId, CancellationToken cancellationToken = default)
    {
        var attachment = await _db.EmailTemplateAttachments.SingleOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (attachment is null) return new EmailTemplateAttachmentCommandResult(EmailTemplateAttachmentCommandStatus.NotFound);

        await _storage.DeleteAsync(attachment.BlobPath, cancellationToken);
        _db.EmailTemplateAttachments.Remove(attachment);
        await _db.SaveChangesAsync(cancellationToken);
        await _auditLogWriter.AppendAsync("email_template_attachment", attachment.Id, "delete", actorId, JsonSerializer.Serialize(new { attachment.FileName }), cancellationToken);
        return new EmailTemplateAttachmentCommandResult(EmailTemplateAttachmentCommandStatus.Success, attachment);
    }
}
