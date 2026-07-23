using InnovationToImpact.Infrastructure.Sla;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class SlaClockServiceTests
{
    [Fact]
    public async Task OpenAsync_MatchingPolicy_CreatesTrackingRow()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var service = new SlaClockService(db);
        var entityId = Guid.NewGuid();

        await service.OpenAsync("evaluation", entityId);

        using var verifyDb = fixture.CreateContext();
        var tracking = await verifyDb.SlaTrackings.SingleAsync(t => t.EntityId == entityId);
        Assert.Null(tracking.ResolvedAt);
        Assert.Null(tracking.BreachedAt);
        var policy = await verifyDb.SlaPolicies.SingleAsync(p => p.Id == tracking.SlaPolicyId);
        Assert.Equal("evaluation", policy.EntityType);
        Assert.Equal(tracking.OpenedAt.AddHours(policy.TargetHours), tracking.TargetAt);
    }

    [Fact]
    public async Task OpenAsync_AlreadyOpenForEntity_DoesNotDuplicate()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var service = new SlaClockService(db);
        var entityId = Guid.NewGuid();

        await service.OpenAsync("evaluation", entityId);
        await service.OpenAsync("evaluation", entityId);

        using var verifyDb = fixture.CreateContext();
        Assert.Single(await verifyDb.SlaTrackings.Where(t => t.EntityId == entityId).ToListAsync());
    }

    [Fact]
    public async Task OpenAsync_NoMatchingPolicy_IsSafeNoOp()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var service = new SlaClockService(db);
        var entityId = Guid.NewGuid();

        await service.OpenAsync("no-such-entity-type", entityId);

        using var verifyDb = fixture.CreateContext();
        Assert.Empty(await verifyDb.SlaTrackings.Where(t => t.EntityId == entityId).ToListAsync());
    }

    [Fact]
    public async Task CloseAsync_OpenTracking_StampsResolvedAt()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var service = new SlaClockService(db);
        var entityId = Guid.NewGuid();
        await service.OpenAsync("committee", entityId);

        await service.CloseAsync("committee", entityId);

        using var verifyDb = fixture.CreateContext();
        var tracking = await verifyDb.SlaTrackings.SingleAsync(t => t.EntityId == entityId);
        Assert.NotNull(tracking.ResolvedAt);
    }

    [Fact]
    public async Task CloseAsync_NothingOpen_IsSafeNoOp()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var service = new SlaClockService(db);

        await service.CloseAsync("committee", Guid.NewGuid());
    }
}
