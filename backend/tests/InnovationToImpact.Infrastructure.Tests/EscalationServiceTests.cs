using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Escalations;
using InnovationToImpact.Infrastructure.Escalations;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class EscalationServiceTests
{
    private static Guid SeedUserAtTier(SqliteContextFixture fixture, string samAccountName, string tierCode, DateTime createdAt)
    {
        using var db = fixture.CreateContext();
        var tierId = db.EscalationTiers.Single(t => t.Code == tierCode).Id;
        var id = Guid.NewGuid();
        db.Users.Add(new User { Id = id, SamAccountName = samAccountName, Email = $"{samAccountName}@gac-demo.sa", FullNameAr = samAccountName, FullNameEn = samAccountName, EscalationTierId = tierId, CreatedAt = createdAt });
        db.SaveChanges();
        return id;
    }

    [Fact]
    public async Task OpenIfAbsentAsync_NoExistingEscalation_CreatesAtTier1WithOwner()
    {
        using var fixture = new SqliteContextFixture();
        var managerId = SeedUserAtTier(fixture, "manager1", "manager", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        using var db = fixture.CreateContext();
        var service = new EscalationService(db);
        var entityId = Guid.NewGuid();

        var escalation = await service.OpenIfAbsentAsync("evaluation", entityId, "سبب", "reason");

        Assert.Equal("manager", escalation.EscalationTier.Code);
        Assert.Equal("open", escalation.EscalationStatus.Code);
        Assert.Equal(managerId, escalation.OwnerId);

        using var verifyDb = fixture.CreateContext();
        var events = await verifyDb.EscalationEvents.Where(e => e.EscalationId == escalation.Id).ToListAsync();
        Assert.Single(events);
        Assert.Equal("opened", events[0].EventType);
    }

    [Fact]
    public async Task OpenIfAbsentAsync_NoUserAtTier_LeavesOwnerNull()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var service = new EscalationService(db);

        var escalation = await service.OpenIfAbsentAsync("committee", Guid.NewGuid(), "سبب", "reason");

        Assert.Null(escalation.OwnerId);
    }

    [Fact]
    public async Task OpenIfAbsentAsync_TwoUsersAtSameTier_PicksEarliestCreated()
    {
        using var fixture = new SqliteContextFixture();
        var laterId = SeedUserAtTier(fixture, "manager-later", "manager", new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));
        var earlierId = SeedUserAtTier(fixture, "manager-earlier", "manager", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        using var db = fixture.CreateContext();
        var service = new EscalationService(db);

        var escalation = await service.OpenIfAbsentAsync("evaluation", Guid.NewGuid(), "سبب", "reason");

        Assert.Equal(earlierId, escalation.OwnerId);
        Assert.NotEqual(laterId, escalation.OwnerId);
    }

    [Fact]
    public async Task OpenIfAbsentAsync_AlreadyOpenForSameEntity_ReturnsExistingNoDuplicate()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var service = new EscalationService(db);
        var entityId = Guid.NewGuid();

        var first = await service.OpenIfAbsentAsync("evaluation", entityId, "أ", "A");
        var second = await service.OpenIfAbsentAsync("evaluation", entityId, "ب", "B");

        Assert.Equal(first.Id, second.Id);

        using var verifyDb = fixture.CreateContext();
        Assert.Single(await verifyDb.Escalations.Where(e => e.EntityType == "evaluation" && e.EntityId == entityId).ToListAsync());
    }

    [Fact]
    public async Task ListAsync_FilterByStatus_ReturnsOnlyMatching()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var service = new EscalationService(db);
        await service.OpenIfAbsentAsync("evaluation", Guid.NewGuid(), "أ", "A");
        await service.OpenIfAbsentAsync("committee", Guid.NewGuid(), "ب", "B");

        var results = await service.ListAsync(new EscalationFilter("open", null, "committee"));

        var only = Assert.Single(results);
        Assert.Equal("committee", only.EntityType);
    }

    [Fact]
    public async Task GetAsync_UnknownId_ReturnsNull()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var service = new EscalationService(db);

        Assert.Null(await service.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task AcknowledgeAsync_OpenEscalation_TransitionsToAcknowledged()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUserAtTier(fixture, "actor1", "manager", DateTime.UtcNow);
        using var db = fixture.CreateContext();
        var service = new EscalationService(db);
        var escalation = await service.OpenIfAbsentAsync("evaluation", Guid.NewGuid(), "أ", "A");

        var result = await service.AcknowledgeAsync(escalation.Id, actorId, "checking now");

        Assert.Equal(EscalationCommandStatus.Success, result.Status);
        Assert.Equal("acknowledged", result.Entity!.EscalationStatus.Code);
        Assert.NotNull(result.Entity.Owner);
        Assert.Equal(actorId, result.Entity.Owner!.Id);

        using var verifyDb = fixture.CreateContext();
        var events = await verifyDb.EscalationEvents.Where(e => e.EscalationId == escalation.Id && e.EventType == "ack").ToListAsync();
        Assert.Single(events);
        Assert.Equal("checking now", events[0].Notes);
    }

    [Fact]
    public async Task AcknowledgeAsync_AlreadyAcknowledged_ReturnsInvalidStatusForAction()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUserAtTier(fixture, "actor2", "manager", DateTime.UtcNow);
        using var db = fixture.CreateContext();
        var service = new EscalationService(db);
        var escalation = await service.OpenIfAbsentAsync("evaluation", Guid.NewGuid(), "أ", "A");
        await service.AcknowledgeAsync(escalation.Id, actorId, null);

        var result = await service.AcknowledgeAsync(escalation.Id, actorId, null);

        Assert.Equal(EscalationCommandStatus.InvalidStatusForAction, result.Status);
    }

    [Fact]
    public async Task AcknowledgeAsync_UnknownId_ReturnsNotFound()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var service = new EscalationService(db);

        var result = await service.AcknowledgeAsync(Guid.NewGuid(), Guid.NewGuid(), null);

        Assert.Equal(EscalationCommandStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task BumpAsync_OpenAtTier1_AdvancesToTier2AndReassignsOwner()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUserAtTier(fixture, "actor3", "manager", DateTime.UtcNow);
        var directorId = SeedUserAtTier(fixture, "director1", "director", DateTime.UtcNow);
        using var db = fixture.CreateContext();
        var service = new EscalationService(db);
        var escalation = await service.OpenIfAbsentAsync("evaluation", Guid.NewGuid(), "أ", "A");

        var result = await service.BumpAsync(escalation.Id, actorId, "escalating");

        Assert.Equal(EscalationCommandStatus.Success, result.Status);
        Assert.Equal("director", result.Entity!.EscalationTier.Code);
        Assert.Equal("open", result.Entity.EscalationStatus.Code);
        Assert.Equal(directorId, result.Entity.OwnerId);

        using var verifyDb = fixture.CreateContext();
        var events = await verifyDb.EscalationEvents.Where(e => e.EscalationId == escalation.Id && e.EventType == "bumped").ToListAsync();
        Assert.Single(events);
    }

    [Fact]
    public async Task BumpAsync_AcknowledgedEscalation_ResetsStatusToOpen()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUserAtTier(fixture, "actor4", "manager", DateTime.UtcNow);
        using var db = fixture.CreateContext();
        var service = new EscalationService(db);
        var escalation = await service.OpenIfAbsentAsync("evaluation", Guid.NewGuid(), "أ", "A");
        await service.AcknowledgeAsync(escalation.Id, actorId, null);

        var result = await service.BumpAsync(escalation.Id, actorId, null);

        Assert.Equal(EscalationCommandStatus.Success, result.Status);
        Assert.Equal("open", result.Entity!.EscalationStatus.Code);
    }

    [Fact]
    public async Task BumpAsync_AlreadyAtMaxTier_ReturnsAlreadyMaxTier()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUserAtTier(fixture, "actor5", "manager", DateTime.UtcNow);
        using var db = fixture.CreateContext();
        var service = new EscalationService(db);
        var escalation = await service.OpenIfAbsentAsync("evaluation", Guid.NewGuid(), "أ", "A");
        await service.BumpAsync(escalation.Id, actorId, null);
        await service.BumpAsync(escalation.Id, actorId, null);

        var result = await service.BumpAsync(escalation.Id, actorId, null);

        Assert.Equal(EscalationCommandStatus.AlreadyMaxTier, result.Status);
    }

    [Fact]
    public async Task ResolveAsync_OpenEscalation_TransitionsToResolvedAndStoresResolution()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUserAtTier(fixture, "actor6", "manager", DateTime.UtcNow);
        using var db = fixture.CreateContext();
        var service = new EscalationService(db);
        var escalation = await service.OpenIfAbsentAsync("evaluation", Guid.NewGuid(), "أ", "A");

        var result = await service.ResolveAsync(escalation.Id, actorId, "تم الحل", "Resolved it");

        Assert.Equal(EscalationCommandStatus.Success, result.Status);
        Assert.Equal("resolved", result.Entity!.EscalationStatus.Code);
        Assert.Equal("تم الحل", result.Entity.ResolutionAr);
        Assert.Equal("Resolved it", result.Entity.ResolutionEn);

        using var verifyDb = fixture.CreateContext();
        var events = await verifyDb.EscalationEvents.Where(e => e.EscalationId == escalation.Id && e.EventType == "resolved").ToListAsync();
        Assert.Single(events);
    }

    [Fact]
    public async Task ResolveAsync_MissingResolutionText_ReturnsResolutionRequired()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUserAtTier(fixture, "actor7", "manager", DateTime.UtcNow);
        using var db = fixture.CreateContext();
        var service = new EscalationService(db);
        var escalation = await service.OpenIfAbsentAsync("evaluation", Guid.NewGuid(), "أ", "A");

        var result = await service.ResolveAsync(escalation.Id, actorId, "", "Resolved it");

        Assert.Equal(EscalationCommandStatus.ResolutionRequired, result.Status);
    }

    [Fact]
    public async Task ResolveAsync_AlreadyResolved_ReturnsInvalidStatusForAction()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUserAtTier(fixture, "actor8", "manager", DateTime.UtcNow);
        using var db = fixture.CreateContext();
        var service = new EscalationService(db);
        var escalation = await service.OpenIfAbsentAsync("evaluation", Guid.NewGuid(), "أ", "A");
        await service.ResolveAsync(escalation.Id, actorId, "أ", "A");

        var result = await service.ResolveAsync(escalation.Id, actorId, "ب", "B");

        Assert.Equal(EscalationCommandStatus.InvalidStatusForAction, result.Status);
    }
}
