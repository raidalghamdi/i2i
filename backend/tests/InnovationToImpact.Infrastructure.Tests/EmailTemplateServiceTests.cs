using InnovationToImpact.Domain.Audit;
using InnovationToImpact.Domain.EmailTemplates;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.EmailTemplates;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class EmailTemplateServiceTests
{
    private sealed class FakeAuditLogWriter : IAuditLogWriter
    {
        public Task<AuditLog> AppendAsync(string entityType, Guid entityId, string action, Guid? actorId, string? payload, CancellationToken cancellationToken = default) =>
            Task.FromResult(new AuditLog { Id = Guid.NewGuid(), EntityType = entityType, EntityId = entityId, Action = action, ActorId = actorId, Payload = payload });
    }

    [Fact]
    public async Task ListAsync_ReturnsAllFourSeededTemplates()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var service = new EmailTemplateService(db, new FakeAuditLogWriter());

        var result = await service.ListAsync();

        Assert.Equal(4, result.Count);
        Assert.Contains(result, t => t.Kind == "invite");
        Assert.Contains(result, t => t.Kind == "accept");
        Assert.Contains(result, t => t.Kind == "reject");
        Assert.Contains(result, t => t.Kind == "reminder");
    }

    [Fact]
    public async Task UpdateAsync_ValidInput_UpdatesSubjectAndBody()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var templateId = db.EmailTemplates.Single(t => t.Kind == "invite").Id;
        var actorId = Guid.NewGuid();
        db.Users.Add(new User { Id = actorId, SamAccountName = "admin1", Email = "admin1@gac-demo.sa", FullNameAr = "م1", FullNameEn = "Admin1" });
        await db.SaveChangesAsync();
        var service = new EmailTemplateService(db, new FakeAuditLogWriter());

        var result = await service.UpdateAsync(templateId, new EmailTemplateInput("موضوع جديد", "New subject", "نص", "Body", true), actorId);

        Assert.Equal(EmailTemplateCommandStatus.Success, result.Status);
        Assert.Equal("New subject", result.Entity!.SubjectEn);
        Assert.True(result.Entity.IsBroadcast);
    }

    [Fact]
    public async Task UpdateAsync_BlankSubject_ReturnsInvalidInput()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var templateId = db.EmailTemplates.Single(t => t.Kind == "invite").Id;
        var service = new EmailTemplateService(db, new FakeAuditLogWriter());

        var result = await service.UpdateAsync(templateId, new EmailTemplateInput("", "New subject", "نص", "Body", false), Guid.NewGuid());

        Assert.Equal(EmailTemplateCommandStatus.InvalidInput, result.Status);
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ReturnsNotFound()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var service = new EmailTemplateService(db, new FakeAuditLogWriter());

        var result = await service.UpdateAsync(Guid.NewGuid(), new EmailTemplateInput("أ", "A", "ب", "B", false), Guid.NewGuid());

        Assert.Equal(EmailTemplateCommandStatus.NotFound, result.Status);
    }
}
