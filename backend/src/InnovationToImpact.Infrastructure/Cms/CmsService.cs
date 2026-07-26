using System.Text.Json;
using InnovationToImpact.Domain.Audit;
using InnovationToImpact.Domain.Cms;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Cms;

public class CmsService : ICmsService
{
    private readonly InnovationDbContext _db;
    private readonly IAuditLogWriter _auditLogWriter;

    public CmsService(InnovationDbContext db, IAuditLogWriter auditLogWriter)
    {
        _db = db;
        _auditLogWriter = auditLogWriter;
    }

    public async Task<IReadOnlyList<CmsBlock>> ListBlocksAsync(CancellationToken cancellationToken = default) =>
        await _db.CmsBlocks.OrderBy(b => b.Key).ToListAsync(cancellationToken);

    public async Task<CmsBlock?> GetBlockAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _db.CmsBlocks.SingleOrDefaultAsync(b => b.Id == id, cancellationToken);

    public async Task<CmsCommandResult<CmsBlock>> CreateBlockAsync(CmsBlockInput input, Guid actorId, CancellationToken cancellationToken = default)
    {
        var duplicate = await _db.CmsBlocks.AnyAsync(b => b.Key == input.Key, cancellationToken);
        if (duplicate) return new CmsCommandResult<CmsBlock>(CmsCommandStatus.DuplicateKey);

        var block = new CmsBlock
        {
            Id = Guid.NewGuid(),
            Key = input.Key,
            ContentAr = input.ContentAr,
            ContentEn = input.ContentEn,
            IsPublished = input.IsPublished,
            UpdatedAt = DateTime.UtcNow,
        };
        _db.CmsBlocks.Add(block);
        await _db.SaveChangesAsync(cancellationToken);
        await _auditLogWriter.AppendAsync("cms_block", block.Id, "create", actorId, JsonSerializer.Serialize(input), cancellationToken);
        return new CmsCommandResult<CmsBlock>(CmsCommandStatus.Success, block);
    }

    public async Task<CmsCommandResult<CmsBlock>> UpdateBlockAsync(Guid id, CmsBlockInput input, Guid actorId, CancellationToken cancellationToken = default)
    {
        var block = await _db.CmsBlocks.SingleOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (block is null) return new CmsCommandResult<CmsBlock>(CmsCommandStatus.NotFound);

        var duplicate = await _db.CmsBlocks.AnyAsync(b => b.Key == input.Key && b.Id != id, cancellationToken);
        if (duplicate) return new CmsCommandResult<CmsBlock>(CmsCommandStatus.DuplicateKey);

        block.Key = input.Key;
        block.ContentAr = input.ContentAr;
        block.ContentEn = input.ContentEn;
        block.IsPublished = input.IsPublished;
        block.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await _auditLogWriter.AppendAsync("cms_block", block.Id, "update", actorId, JsonSerializer.Serialize(input), cancellationToken);
        return new CmsCommandResult<CmsBlock>(CmsCommandStatus.Success, block);
    }

    public async Task<CmsCommandResult<CmsBlock>> DeleteBlockAsync(Guid id, Guid actorId, CancellationToken cancellationToken = default)
    {
        var block = await _db.CmsBlocks.SingleOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (block is null) return new CmsCommandResult<CmsBlock>(CmsCommandStatus.NotFound);

        _db.CmsBlocks.Remove(block);
        await _db.SaveChangesAsync(cancellationToken);
        await _auditLogWriter.AppendAsync("cms_block", block.Id, "delete", actorId, JsonSerializer.Serialize(new { block.Key }), cancellationToken);
        return new CmsCommandResult<CmsBlock>(CmsCommandStatus.Success, block);
    }

    public async Task<IReadOnlyList<CmsContent>> ListContentAsync(CancellationToken cancellationToken = default) =>
        await _db.CmsContents.OrderBy(c => c.Slug).ToListAsync(cancellationToken);

    public async Task<CmsContent?> GetContentAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _db.CmsContents.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<CmsCommandResult<CmsContent>> CreateContentAsync(CmsContentInput input, Guid actorId, CancellationToken cancellationToken = default)
    {
        var duplicate = await _db.CmsContents.AnyAsync(c => c.Slug == input.Slug, cancellationToken);
        if (duplicate) return new CmsCommandResult<CmsContent>(CmsCommandStatus.DuplicateKey);

        var content = new CmsContent
        {
            Id = Guid.NewGuid(),
            Slug = input.Slug,
            TitleAr = input.TitleAr,
            TitleEn = input.TitleEn,
            BodyAr = input.BodyAr,
            BodyEn = input.BodyEn,
            IsPublished = input.IsPublished,
            UpdatedAt = DateTime.UtcNow,
        };
        _db.CmsContents.Add(content);
        await _db.SaveChangesAsync(cancellationToken);
        await _auditLogWriter.AppendAsync("cms_content", content.Id, "create", actorId, JsonSerializer.Serialize(input), cancellationToken);
        return new CmsCommandResult<CmsContent>(CmsCommandStatus.Success, content);
    }

    public async Task<CmsCommandResult<CmsContent>> UpdateContentAsync(Guid id, CmsContentInput input, Guid actorId, CancellationToken cancellationToken = default)
    {
        var content = await _db.CmsContents.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (content is null) return new CmsCommandResult<CmsContent>(CmsCommandStatus.NotFound);

        var duplicate = await _db.CmsContents.AnyAsync(c => c.Slug == input.Slug && c.Id != id, cancellationToken);
        if (duplicate) return new CmsCommandResult<CmsContent>(CmsCommandStatus.DuplicateKey);

        content.Slug = input.Slug;
        content.TitleAr = input.TitleAr;
        content.TitleEn = input.TitleEn;
        content.BodyAr = input.BodyAr;
        content.BodyEn = input.BodyEn;
        content.IsPublished = input.IsPublished;
        content.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await _auditLogWriter.AppendAsync("cms_content", content.Id, "update", actorId, JsonSerializer.Serialize(input), cancellationToken);
        return new CmsCommandResult<CmsContent>(CmsCommandStatus.Success, content);
    }

    public async Task<CmsCommandResult<CmsContent>> DeleteContentAsync(Guid id, Guid actorId, CancellationToken cancellationToken = default)
    {
        var content = await _db.CmsContents.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (content is null) return new CmsCommandResult<CmsContent>(CmsCommandStatus.NotFound);

        _db.CmsContents.Remove(content);
        await _db.SaveChangesAsync(cancellationToken);
        await _auditLogWriter.AppendAsync("cms_content", content.Id, "delete", actorId, JsonSerializer.Serialize(new { content.Slug }), cancellationToken);
        return new CmsCommandResult<CmsContent>(CmsCommandStatus.Success, content);
    }

    public async Task<IReadOnlyList<ContentString>> ListStringsAsync(CancellationToken cancellationToken = default) =>
        await _db.ContentStrings.OrderBy(s => s.Key).ToListAsync(cancellationToken);

    public async Task<ContentString?> GetStringAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _db.ContentStrings.SingleOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<CmsCommandResult<ContentString>> CreateStringAsync(ContentStringInput input, Guid actorId, CancellationToken cancellationToken = default)
    {
        var duplicate = await _db.ContentStrings.AnyAsync(s => s.Key == input.Key, cancellationToken);
        if (duplicate) return new CmsCommandResult<ContentString>(CmsCommandStatus.DuplicateKey);

        var contentString = new ContentString
        {
            Id = Guid.NewGuid(),
            Key = input.Key,
            ValueAr = input.ValueAr,
            ValueEn = input.ValueEn,
            UpdatedAt = DateTime.UtcNow,
        };
        _db.ContentStrings.Add(contentString);
        await _db.SaveChangesAsync(cancellationToken);
        await _auditLogWriter.AppendAsync("content_string", contentString.Id, "create", actorId, JsonSerializer.Serialize(input), cancellationToken);
        return new CmsCommandResult<ContentString>(CmsCommandStatus.Success, contentString);
    }

    public async Task<CmsCommandResult<ContentString>> UpdateStringAsync(Guid id, ContentStringInput input, Guid actorId, CancellationToken cancellationToken = default)
    {
        var contentString = await _db.ContentStrings.SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (contentString is null) return new CmsCommandResult<ContentString>(CmsCommandStatus.NotFound);

        var duplicate = await _db.ContentStrings.AnyAsync(s => s.Key == input.Key && s.Id != id, cancellationToken);
        if (duplicate) return new CmsCommandResult<ContentString>(CmsCommandStatus.DuplicateKey);

        contentString.Key = input.Key;
        contentString.ValueAr = input.ValueAr;
        contentString.ValueEn = input.ValueEn;
        contentString.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        await _auditLogWriter.AppendAsync("content_string", contentString.Id, "update", actorId, JsonSerializer.Serialize(input), cancellationToken);
        return new CmsCommandResult<ContentString>(CmsCommandStatus.Success, contentString);
    }

    public async Task<CmsCommandResult<ContentString>> DeleteStringAsync(Guid id, Guid actorId, CancellationToken cancellationToken = default)
    {
        var contentString = await _db.ContentStrings.SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (contentString is null) return new CmsCommandResult<ContentString>(CmsCommandStatus.NotFound);

        _db.ContentStrings.Remove(contentString);
        await _db.SaveChangesAsync(cancellationToken);
        await _auditLogWriter.AppendAsync("content_string", contentString.Id, "delete", actorId, JsonSerializer.Serialize(new { contentString.Key }), cancellationToken);
        return new CmsCommandResult<ContentString>(CmsCommandStatus.Success, contentString);
    }
}
