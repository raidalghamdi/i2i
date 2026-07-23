using InnovationToImpact.Domain.Cms;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Audit;
using InnovationToImpact.Infrastructure.Cms;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class CmsServiceTests
{
    private static Guid SeedUser(SqliteContextFixture fixture, string samAccountName)
    {
        using var db = fixture.CreateContext();
        var id = Guid.NewGuid();
        db.Users.Add(new User { Id = id, SamAccountName = samAccountName, Email = $"{samAccountName}@gac-demo.sa", FullNameAr = samAccountName, FullNameEn = samAccountName });
        db.SaveChanges();
        return id;
    }

    [Fact]
    public async Task CreateBlockAsync_NewKey_CreatesAndAppendsAuditEntry()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUser(fixture, "admin1");
        using var db = fixture.CreateContext();
        var service = new CmsService(db, new AuditLogWriter(db));

        var result = await service.CreateBlockAsync(new CmsBlockInput("welcome-banner", "أهلا", "Welcome", true), actorId);

        Assert.Equal(CmsCommandStatus.Success, result.Status);
        Assert.Equal("welcome-banner", result.Entity!.Key);

        using var verifyDb = fixture.CreateContext();
        Assert.Single(await verifyDb.CmsBlocks.Where(b => b.Key == "welcome-banner").ToListAsync());
        var auditEntry = Assert.Single(await verifyDb.AuditLogs.Where(a => a.EntityType == "cms_block" && a.Action == "create").ToListAsync());
        Assert.Equal(result.Entity.Id, auditEntry.EntityId);
        Assert.Equal(actorId, auditEntry.ActorId);
    }

    [Fact]
    public async Task CreateBlockAsync_DuplicateKey_ReturnsDuplicateKeyAndAppendsNoAuditEntry()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUser(fixture, "admin2");
        using var db = fixture.CreateContext();
        var service = new CmsService(db, new AuditLogWriter(db));
        await service.CreateBlockAsync(new CmsBlockInput("footer-text", "أ", "F", true), actorId);

        var result = await service.CreateBlockAsync(new CmsBlockInput("footer-text", "ب", "G", true), actorId);

        Assert.Equal(CmsCommandStatus.DuplicateKey, result.Status);

        using var verifyDb = fixture.CreateContext();
        Assert.Empty(await verifyDb.AuditLogs.Where(a => a.EntityType == "cms_block" && a.Action == "create" && a.Payload!.Contains("\"ب\"")).ToListAsync());
    }

    [Fact]
    public async Task UpdateBlockAsync_ExistingId_UpdatesAndAppendsAuditEntry()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUser(fixture, "admin3");
        using var db = fixture.CreateContext();
        var service = new CmsService(db, new AuditLogWriter(db));
        var created = await service.CreateBlockAsync(new CmsBlockInput("hero-title", "أ", "E", true), actorId);

        var result = await service.UpdateBlockAsync(created.Entity!.Id, new CmsBlockInput("hero-title", "ب", "Updated", false), actorId);

        Assert.Equal(CmsCommandStatus.Success, result.Status);
        Assert.Equal("Updated", result.Entity!.ContentEn);
        Assert.False(result.Entity.IsPublished);

        using var verifyDb = fixture.CreateContext();
        Assert.Single(await verifyDb.AuditLogs.Where(a => a.EntityType == "cms_block" && a.Action == "update").ToListAsync());
    }

    [Fact]
    public async Task UpdateBlockAsync_CollidesWithAnotherRowsKey_ReturnsDuplicateKey()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUser(fixture, "admin4");
        using var db = fixture.CreateContext();
        var service = new CmsService(db, new AuditLogWriter(db));
        await service.CreateBlockAsync(new CmsBlockInput("key-a", "أ", "A", true), actorId);
        var second = await service.CreateBlockAsync(new CmsBlockInput("key-b", "ب", "B", true), actorId);

        var result = await service.UpdateBlockAsync(second.Entity!.Id, new CmsBlockInput("key-a", "ب", "B2", true), actorId);

        Assert.Equal(CmsCommandStatus.DuplicateKey, result.Status);
    }

    [Fact]
    public async Task UpdateBlockAsync_UnknownId_ReturnsNotFound()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUser(fixture, "admin5");
        using var db = fixture.CreateContext();
        var service = new CmsService(db, new AuditLogWriter(db));

        var result = await service.UpdateBlockAsync(Guid.NewGuid(), new CmsBlockInput("x", "أ", "X", true), actorId);

        Assert.Equal(CmsCommandStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task DeleteBlockAsync_ExistingId_DeletesAndAppendsAuditEntry()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUser(fixture, "admin6");
        using var db = fixture.CreateContext();
        var service = new CmsService(db, new AuditLogWriter(db));
        var created = await service.CreateBlockAsync(new CmsBlockInput("to-delete", "أ", "D", true), actorId);

        var result = await service.DeleteBlockAsync(created.Entity!.Id, actorId);

        Assert.Equal(CmsCommandStatus.Success, result.Status);

        using var verifyDb = fixture.CreateContext();
        Assert.Empty(await verifyDb.CmsBlocks.Where(b => b.Key == "to-delete").ToListAsync());
        Assert.Single(await verifyDb.AuditLogs.Where(a => a.EntityType == "cms_block" && a.Action == "delete").ToListAsync());
    }

    [Fact]
    public async Task DeleteBlockAsync_UnknownId_ReturnsNotFound()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUser(fixture, "admin7");
        using var db = fixture.CreateContext();
        var service = new CmsService(db, new AuditLogWriter(db));

        var result = await service.DeleteBlockAsync(Guid.NewGuid(), actorId);

        Assert.Equal(CmsCommandStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task ListBlocksAsync_ReturnsAllBlocksOrderedByKey()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUser(fixture, "admin8");
        using var db = fixture.CreateContext();
        var service = new CmsService(db, new AuditLogWriter(db));
        await service.CreateBlockAsync(new CmsBlockInput("zeta", "أ", "Z", true), actorId);
        await service.CreateBlockAsync(new CmsBlockInput("alpha", "ب", "A", true), actorId);

        var blocks = await service.ListBlocksAsync();

        Assert.Equal(new[] { "alpha", "zeta" }, blocks.Select(b => b.Key));
    }

    [Fact]
    public async Task GetBlockAsync_UnknownId_ReturnsNull()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var service = new CmsService(db, new AuditLogWriter(db));

        Assert.Null(await service.GetBlockAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateContentAsync_NewSlug_CreatesAndAppendsAuditEntry()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUser(fixture, "content-admin1");
        using var db = fixture.CreateContext();
        var service = new CmsService(db, new AuditLogWriter(db));

        var result = await service.CreateContentAsync(new CmsContentInput("terms-and-conditions", "الشروط", "Terms", "نص", "Body", true), actorId);

        Assert.Equal(CmsCommandStatus.Success, result.Status);
        Assert.Equal("terms-and-conditions", result.Entity!.Slug);
        Assert.Null(result.Entity.PublishedAt);

        using var verifyDb = fixture.CreateContext();
        Assert.Single(await verifyDb.CmsContents.Where(c => c.Slug == "terms-and-conditions").ToListAsync());
        var auditEntry = Assert.Single(await verifyDb.AuditLogs.Where(a => a.EntityType == "cms_content" && a.Action == "create").ToListAsync());
        Assert.Equal(result.Entity.Id, auditEntry.EntityId);
    }

    [Fact]
    public async Task CreateContentAsync_DuplicateSlug_ReturnsDuplicateKey()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUser(fixture, "content-admin2");
        using var db = fixture.CreateContext();
        var service = new CmsService(db, new AuditLogWriter(db));
        await service.CreateContentAsync(new CmsContentInput("faq", "أ", "F", "ب", "B", true), actorId);

        var result = await service.CreateContentAsync(new CmsContentInput("faq", "ج", "F2", "د", "B2", true), actorId);

        Assert.Equal(CmsCommandStatus.DuplicateKey, result.Status);
    }

    [Fact]
    public async Task UpdateContentAsync_ExistingId_UpdatesAndAppendsAuditEntry()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUser(fixture, "content-admin3");
        using var db = fixture.CreateContext();
        var service = new CmsService(db, new AuditLogWriter(db));
        var created = await service.CreateContentAsync(new CmsContentInput("about", "أ", "About", "ب", "Body", true), actorId);

        var result = await service.UpdateContentAsync(created.Entity!.Id, new CmsContentInput("about", "أ2", "About Updated", "ب2", "Body2", false), actorId);

        Assert.Equal(CmsCommandStatus.Success, result.Status);
        Assert.Equal("About Updated", result.Entity!.TitleEn);
        Assert.False(result.Entity.IsPublished);

        using var verifyDb = fixture.CreateContext();
        Assert.Single(await verifyDb.AuditLogs.Where(a => a.EntityType == "cms_content" && a.Action == "update").ToListAsync());
    }

    [Fact]
    public async Task UpdateContentAsync_CollidesWithAnotherRowsSlug_ReturnsDuplicateKey()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUser(fixture, "content-admin4");
        using var db = fixture.CreateContext();
        var service = new CmsService(db, new AuditLogWriter(db));
        await service.CreateContentAsync(new CmsContentInput("slug-a", "أ", "A", "ب", "B", true), actorId);
        var second = await service.CreateContentAsync(new CmsContentInput("slug-b", "ج", "C", "د", "D", true), actorId);

        var result = await service.UpdateContentAsync(second.Entity!.Id, new CmsContentInput("slug-a", "ج", "C2", "د", "D2", true), actorId);

        Assert.Equal(CmsCommandStatus.DuplicateKey, result.Status);
    }

    [Fact]
    public async Task UpdateContentAsync_UnknownId_ReturnsNotFound()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUser(fixture, "content-admin5");
        using var db = fixture.CreateContext();
        var service = new CmsService(db, new AuditLogWriter(db));

        var result = await service.UpdateContentAsync(Guid.NewGuid(), new CmsContentInput("x", "أ", "X", "ب", "Y", true), actorId);

        Assert.Equal(CmsCommandStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task DeleteContentAsync_ExistingId_DeletesAndAppendsAuditEntry()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUser(fixture, "content-admin6");
        using var db = fixture.CreateContext();
        var service = new CmsService(db, new AuditLogWriter(db));
        var created = await service.CreateContentAsync(new CmsContentInput("to-delete", "أ", "D", "ب", "E", true), actorId);

        var result = await service.DeleteContentAsync(created.Entity!.Id, actorId);

        Assert.Equal(CmsCommandStatus.Success, result.Status);

        using var verifyDb = fixture.CreateContext();
        Assert.Empty(await verifyDb.CmsContents.Where(c => c.Slug == "to-delete").ToListAsync());
        Assert.Single(await verifyDb.AuditLogs.Where(a => a.EntityType == "cms_content" && a.Action == "delete").ToListAsync());
    }

    [Fact]
    public async Task DeleteContentAsync_UnknownId_ReturnsNotFound()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUser(fixture, "content-admin7");
        using var db = fixture.CreateContext();
        var service = new CmsService(db, new AuditLogWriter(db));

        var result = await service.DeleteContentAsync(Guid.NewGuid(), actorId);

        Assert.Equal(CmsCommandStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task ListContentAsync_ReturnsAllContentOrderedBySlug()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUser(fixture, "content-admin8");
        using var db = fixture.CreateContext();
        var service = new CmsService(db, new AuditLogWriter(db));
        await service.CreateContentAsync(new CmsContentInput("zeta-page", "أ", "Z", "ب", "B", true), actorId);
        await service.CreateContentAsync(new CmsContentInput("alpha-page", "ج", "A", "د", "D", true), actorId);

        var content = await service.ListContentAsync();

        // "terms" is seeded structural CMS content (CmsContentConfiguration.HasData) and sorts between the two test rows.
        Assert.Equal(new[] { "alpha-page", "terms", "zeta-page" }, content.Select(c => c.Slug));
    }

    [Fact]
    public async Task CreateStringAsync_NewKey_CreatesAndAppendsAuditEntry()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUser(fixture, "string-admin1");
        using var db = fixture.CreateContext();
        var service = new CmsService(db, new AuditLogWriter(db));

        var result = await service.CreateStringAsync(new ContentStringInput("nav.home", "الرئيسية", "Home"), actorId);

        Assert.Equal(CmsCommandStatus.Success, result.Status);
        Assert.Equal("nav.home", result.Entity!.Key);

        using var verifyDb = fixture.CreateContext();
        Assert.Single(await verifyDb.ContentStrings.Where(s => s.Key == "nav.home").ToListAsync());
        var auditEntry = Assert.Single(await verifyDb.AuditLogs.Where(a => a.EntityType == "content_string" && a.Action == "create").ToListAsync());
        Assert.Equal(result.Entity.Id, auditEntry.EntityId);
    }

    [Fact]
    public async Task CreateStringAsync_DuplicateKey_ReturnsDuplicateKey()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUser(fixture, "string-admin2");
        using var db = fixture.CreateContext();
        var service = new CmsService(db, new AuditLogWriter(db));
        await service.CreateStringAsync(new ContentStringInput("nav.about", "أ", "A"), actorId);

        var result = await service.CreateStringAsync(new ContentStringInput("nav.about", "ب", "B"), actorId);

        Assert.Equal(CmsCommandStatus.DuplicateKey, result.Status);
    }

    [Fact]
    public async Task UpdateStringAsync_ExistingId_UpdatesAndAppendsAuditEntry()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUser(fixture, "string-admin3");
        using var db = fixture.CreateContext();
        var service = new CmsService(db, new AuditLogWriter(db));
        var created = await service.CreateStringAsync(new ContentStringInput("nav.contact", "أ", "Contact"), actorId);

        var result = await service.UpdateStringAsync(created.Entity!.Id, new ContentStringInput("nav.contact", "ب", "Contact Us"), actorId);

        Assert.Equal(CmsCommandStatus.Success, result.Status);
        Assert.Equal("Contact Us", result.Entity!.ValueEn);

        using var verifyDb = fixture.CreateContext();
        Assert.Single(await verifyDb.AuditLogs.Where(a => a.EntityType == "content_string" && a.Action == "update").ToListAsync());
    }

    [Fact]
    public async Task UpdateStringAsync_CollidesWithAnotherRowsKey_ReturnsDuplicateKey()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUser(fixture, "string-admin4");
        using var db = fixture.CreateContext();
        var service = new CmsService(db, new AuditLogWriter(db));
        await service.CreateStringAsync(new ContentStringInput("key-a", "أ", "A"), actorId);
        var second = await service.CreateStringAsync(new ContentStringInput("key-b", "ب", "B"), actorId);

        var result = await service.UpdateStringAsync(second.Entity!.Id, new ContentStringInput("key-a", "ب", "B2"), actorId);

        Assert.Equal(CmsCommandStatus.DuplicateKey, result.Status);
    }

    [Fact]
    public async Task UpdateStringAsync_UnknownId_ReturnsNotFound()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUser(fixture, "string-admin5");
        using var db = fixture.CreateContext();
        var service = new CmsService(db, new AuditLogWriter(db));

        var result = await service.UpdateStringAsync(Guid.NewGuid(), new ContentStringInput("x", "أ", "X"), actorId);

        Assert.Equal(CmsCommandStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task DeleteStringAsync_ExistingId_DeletesAndAppendsAuditEntry()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUser(fixture, "string-admin6");
        using var db = fixture.CreateContext();
        var service = new CmsService(db, new AuditLogWriter(db));
        var created = await service.CreateStringAsync(new ContentStringInput("to-delete", "أ", "D"), actorId);

        var result = await service.DeleteStringAsync(created.Entity!.Id, actorId);

        Assert.Equal(CmsCommandStatus.Success, result.Status);

        using var verifyDb = fixture.CreateContext();
        Assert.Empty(await verifyDb.ContentStrings.Where(s => s.Key == "to-delete").ToListAsync());
        Assert.Single(await verifyDb.AuditLogs.Where(a => a.EntityType == "content_string" && a.Action == "delete").ToListAsync());
    }

    [Fact]
    public async Task DeleteStringAsync_UnknownId_ReturnsNotFound()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUser(fixture, "string-admin7");
        using var db = fixture.CreateContext();
        var service = new CmsService(db, new AuditLogWriter(db));

        var result = await service.DeleteStringAsync(Guid.NewGuid(), actorId);

        Assert.Equal(CmsCommandStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task ListStringsAsync_ReturnsAllStringsOrderedByKey()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUser(fixture, "string-admin8");
        using var db = fixture.CreateContext();
        var service = new CmsService(db, new AuditLogWriter(db));
        await service.CreateStringAsync(new ContentStringInput("zeta.key", "أ", "Z"), actorId);
        await service.CreateStringAsync(new ContentStringInput("alpha.key", "ب", "A"), actorId);

        var strings = await service.ListStringsAsync();

        Assert.Equal(new[] { "alpha.key", "zeta.key" }, strings.Select(s => s.Key));
    }
}
