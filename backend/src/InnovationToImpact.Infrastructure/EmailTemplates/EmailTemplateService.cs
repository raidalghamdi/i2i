using System.Text.Json;
using InnovationToImpact.Domain.Audit;
using InnovationToImpact.Domain.EmailTemplates;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.EmailTemplates;

public class EmailTemplateService : IEmailTemplateService
{
    private readonly InnovationDbContext _db;
    private readonly IAuditLogWriter _auditLogWriter;

    public EmailTemplateService(InnovationDbContext db, IAuditLogWriter auditLogWriter)
    {
        _db = db;
        _auditLogWriter = auditLogWriter;
    }

    public async Task<IReadOnlyList<EmailTemplate>> ListAsync(CancellationToken cancellationToken = default) =>
        await _db.EmailTemplates.OrderBy(t => t.Kind).ToListAsync(cancellationToken);

    public async Task<EmailTemplateCommandResult> UpdateAsync(Guid id, EmailTemplateInput input, Guid actorId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.SubjectAr) || string.IsNullOrWhiteSpace(input.SubjectEn) ||
            string.IsNullOrWhiteSpace(input.BodyAr) || string.IsNullOrWhiteSpace(input.BodyEn))
            return new EmailTemplateCommandResult(EmailTemplateCommandStatus.InvalidInput);

        var template = await _db.EmailTemplates.SingleOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (template is null) return new EmailTemplateCommandResult(EmailTemplateCommandStatus.NotFound);

        template.SubjectAr = input.SubjectAr;
        template.SubjectEn = input.SubjectEn;
        template.BodyAr = input.BodyAr;
        template.BodyEn = input.BodyEn;
        template.IsBroadcast = input.IsBroadcast;
        await _db.SaveChangesAsync(cancellationToken);
        await _auditLogWriter.AppendAsync("email_template", template.Id, "update", actorId, JsonSerializer.Serialize(input), cancellationToken);
        return new EmailTemplateCommandResult(EmailTemplateCommandStatus.Success, template);
    }
}
