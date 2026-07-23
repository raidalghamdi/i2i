using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Sla;
using InnovationToImpact.Infrastructure.Data;
using InnovationToImpact.Infrastructure.Sla;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

// Deliberately NOT IClassFixture<SqliteContextFixture> -- these tests assert absolute
// SlaScanResult counts, which only hold against tables no other test method has already
// written rows into, and xUnit does not guarantee [Fact] execution order. A fresh fixture
// per test method isolates each test's tables, matching the fix already applied in the
// Audit Hash-Chain Reimplementation and Email Outbox Worker plans.
public class SlaScannerTests : IDisposable
{
    private readonly SqliteContextFixture _fixture = new();

    private static Guid SeedPolicy(InnovationDbContext db, int targetHours = 24, int warnAtPct = 80)
    {
        var policy = new SlaPolicy
        {
            Id = Guid.NewGuid(),
            EntityType = "idea",
            FromState = "submitted",
            ToState = "reviewed",
            TargetHours = targetHours,
            WarnAtPct = warnAtPct,
        };
        db.SlaPolicies.Add(policy);
        db.SaveChanges();
        return policy.Id;
    }

    private static Guid SeedTracking(
        InnovationDbContext db,
        Guid policyId,
        DateTime openedAt,
        DateTime targetAt,
        DateTime? breachedAt = null,
        DateTime? resolvedAt = null)
    {
        var tracking = new SlaTracking
        {
            Id = Guid.NewGuid(),
            SlaPolicyId = policyId,
            EntityId = Guid.NewGuid(),
            OpenedAt = openedAt,
            TargetAt = targetAt,
            BreachedAt = breachedAt,
            ResolvedAt = resolvedAt,
        };
        db.SlaTrackings.Add(tracking);
        db.SaveChanges();
        return tracking.Id;
    }

    [Fact]
    public async Task RowPastTargetAt_IsNewlyBreached()
    {
        using var db = _fixture.CreateContext();
        var policyId = SeedPolicy(db, targetHours: 24);
        var id = SeedTracking(db, policyId, openedAt: DateTime.UtcNow.AddHours(-30), targetAt: DateTime.UtcNow.AddHours(-6));

        var scanner = new SlaScanner(db);
        var result = await scanner.ScanAsync(CancellationToken.None);

        Assert.Equal(1, result.Scanned);
        Assert.Equal(1, result.NewlyBreached);
        Assert.Equal(0, result.ApproachingBreach);
        Assert.Equal(new[] { id }, result.NewlyBreachedTrackingIds);

        var updated = await db.SlaTrackings.SingleAsync(t => t.Id == id);
        Assert.NotNull(updated.BreachedAt);
    }

    [Fact]
    public async Task RowWithinWarnThreshold_IsApproachingBreach_NoMutation()
    {
        using var db = _fixture.CreateContext();
        // targetHours=24, warnAtPct=80 -> warn threshold at 19.2h elapsed. Opened 20h ago with
        // a target 4h from now puts elapsed% at ~83%, past warn but not yet at target.
        var policyId = SeedPolicy(db, targetHours: 24, warnAtPct: 80);
        var id = SeedTracking(db, policyId, openedAt: DateTime.UtcNow.AddHours(-20), targetAt: DateTime.UtcNow.AddHours(4));

        var scanner = new SlaScanner(db);
        var result = await scanner.ScanAsync(CancellationToken.None);

        Assert.Equal(1, result.Scanned);
        Assert.Equal(0, result.NewlyBreached);
        Assert.Equal(1, result.ApproachingBreach);
        Assert.Empty(result.NewlyBreachedTrackingIds);

        var updated = await db.SlaTrackings.SingleAsync(t => t.Id == id);
        Assert.Null(updated.BreachedAt);
    }

    [Fact]
    public async Task AlreadyBreachedRow_IsNotDoubleCounted_NotRemutated()
    {
        using var db = _fixture.CreateContext();
        var policyId = SeedPolicy(db);
        var originalBreachedAt = DateTime.UtcNow.AddHours(-1);
        var id = SeedTracking(db, policyId, openedAt: DateTime.UtcNow.AddHours(-30), targetAt: DateTime.UtcNow.AddHours(-6), breachedAt: originalBreachedAt);

        var scanner = new SlaScanner(db);
        var result = await scanner.ScanAsync(CancellationToken.None);

        Assert.Equal(1, result.Scanned);
        Assert.Equal(0, result.NewlyBreached);
        Assert.Equal(0, result.ApproachingBreach);
        Assert.Empty(result.NewlyBreachedTrackingIds);

        var updated = await db.SlaTrackings.SingleAsync(t => t.Id == id);
        Assert.Equal(originalBreachedAt, updated.BreachedAt);
    }

    [Fact]
    public async Task ResolvedRow_IsExcludedEntirely_EvenIfPastTargetAt()
    {
        using var db = _fixture.CreateContext();
        var policyId = SeedPolicy(db);
        SeedTracking(db, policyId, openedAt: DateTime.UtcNow.AddHours(-30), targetAt: DateTime.UtcNow.AddHours(-6), resolvedAt: DateTime.UtcNow.AddHours(-2));

        var scanner = new SlaScanner(db);
        var result = await scanner.ScanAsync(CancellationToken.None);

        Assert.Equal(0, result.Scanned);
        Assert.Equal(0, result.NewlyBreached);
        Assert.Equal(0, result.ApproachingBreach);
        Assert.Empty(result.NewlyBreachedTrackingIds);
    }

    [Fact]
    public async Task RowEarlyInWindow_IsNotCountedInEitherBucket()
    {
        using var db = _fixture.CreateContext();
        var policyId = SeedPolicy(db, targetHours: 24, warnAtPct: 80);
        SeedTracking(db, policyId, openedAt: DateTime.UtcNow.AddHours(-1), targetAt: DateTime.UtcNow.AddHours(23));

        var scanner = new SlaScanner(db);
        var result = await scanner.ScanAsync(CancellationToken.None);

        Assert.Equal(1, result.Scanned);
        Assert.Equal(0, result.NewlyBreached);
        Assert.Equal(0, result.ApproachingBreach);
        Assert.Empty(result.NewlyBreachedTrackingIds);
    }

    public void Dispose() => _fixture.Dispose();
}
