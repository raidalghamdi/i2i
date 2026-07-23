using InnovationToImpact.Domain.Audit;
using InnovationToImpact.Domain.EmailTemplates;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.EmailTemplates;
using InnovationToImpact.Infrastructure.Storage;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class EmailTemplateAttachmentServiceTests : IDisposable
{
    private readonly string _rootPath = Path.Combine(Path.GetTempPath(), $"email-template-attachment-test-{Guid.NewGuid():N}");

    private sealed class FakeAuditLogWriter : IAuditLogWriter
    {
        public Task<AuditLog> AppendAsync(string entityType, Guid entityId, string action, Guid? actorId, string? payload, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AuditLog { Id = Guid.NewGuid(), EntityType = entityType, EntityId = entityId, Action = action, ActorId = actorId, Payload = payload });
    }

    [Fact]
    public async Task UploadAsync_ValidTemplate_SavesFileAndPersistsRow()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var templateId = db.EmailTemplates.Single(t => t.Kind == "invite").Id;
        var actorId = Guid.NewGuid();
        db.Users.Add(new User { Id = actorId, SamAccountName = "admin1", Email = "admin1@gac-demo.sa", FullNameAr = "م1", FullNameEn = "Admin1" });
        await db.SaveChangesAsync();
        var storage = new LocalDiskFileStorage(_rootPath);
        var service = new EmailTemplateAttachmentService(db, storage, new FakeAuditLogWriter());

        var result = await service.UploadAsync(templateId, "brochure.pdf", "application/pdf", new byte[] { 1, 2, 3 }, actorId);

        Assert.Equal(EmailTemplateAttachmentCommandStatus.Success, result.Status);
        Assert.Equal("brochure.pdf", result.Entity!.FileName);
        Assert.True(File.Exists(result.Entity.BlobPath));
    }

    [Fact]
    public async Task UploadAsync_TemplateDoesNotExist_ReturnsTemplateNotFound()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var storage = new LocalDiskFileStorage(_rootPath);
        var service = new EmailTemplateAttachmentService(db, storage, new FakeAuditLogWriter());

        var result = await service.UploadAsync(Guid.NewGuid(), "a.pdf", "application/pdf", new byte[] { 1 }, Guid.NewGuid());

        Assert.Equal(EmailTemplateAttachmentCommandStatus.TemplateNotFound, result.Status);
    }

    [Fact]
    public async Task DeleteAsync_RemovesRowAndFile()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var templateId = db.EmailTemplates.Single(t => t.Kind == "invite").Id;
        var actorId = Guid.NewGuid();
        db.Users.Add(new User { Id = actorId, SamAccountName = "admin1", Email = "admin1@gac-demo.sa", FullNameAr = "م1", FullNameEn = "Admin1" });
        await db.SaveChangesAsync();
        var storage = new LocalDiskFileStorage(_rootPath);
        var service = new EmailTemplateAttachmentService(db, storage, new FakeAuditLogWriter());
        var uploaded = await service.UploadAsync(templateId, "a.pdf", "application/pdf", new byte[] { 1 }, actorId);
        var blobPath = uploaded.Entity!.BlobPath;

        var result = await service.DeleteAsync(uploaded.Entity.Id, actorId);

        Assert.Equal(EmailTemplateAttachmentCommandStatus.Success, result.Status);
        Assert.False(db.EmailTemplateAttachments.Any(a => a.Id == uploaded.Entity.Id));
        Assert.False(File.Exists(blobPath));
    }

    [Fact]
    public async Task DeleteAsync_NotFound_ReturnsNotFound()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var storage = new LocalDiskFileStorage(_rootPath);
        var service = new EmailTemplateAttachmentService(db, storage, new FakeAuditLogWriter());

        var result = await service.DeleteAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(EmailTemplateAttachmentCommandStatus.NotFound, result.Status);
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath)) Directory.Delete(_rootPath, recursive: true);
    }
}
