using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class SlaTrackingConfigurationTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public SlaTrackingConfigurationTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    private static Guid SeedPolicy(InnovationDbContext context, string suffix)
    {
        var policyId = Guid.NewGuid();
        context.SlaPolicies.Add(new SlaPolicy
        {
            Id = policyId,
            EntityType = "idea",
            FromState = $"state-a-{suffix}",
            ToState = $"state-b-{suffix}",
            TargetHours = 24,
            WarnAtPct = 80,
        });
        context.SaveChanges();
        return policyId;
    }

    [Fact]
    public void SavesOpenTrackingRowForAPolicy()
    {
        Guid trackingId;
        Guid trackedEntityId;

        using (var context = _fixture.CreateContext())
        {
            var policyId = SeedPolicy(context, "t3a");
            trackedEntityId = Guid.NewGuid();

            var tracking = new SlaTracking
            {
                Id = Guid.NewGuid(),
                SlaPolicyId = policyId,
                EntityId = trackedEntityId,
                TargetAt = DateTime.UtcNow.AddHours(24),
            };
            trackingId = tracking.Id;

            context.SlaTrackings.Add(tracking);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var tracking = context.SlaTrackings
                .Include(t => t.SlaPolicy)
                .Single(t => t.Id == trackingId);
            Assert.Equal("idea", tracking.SlaPolicy.EntityType);
            Assert.Equal(trackedEntityId, tracking.EntityId);
            Assert.Null(tracking.BreachedAt);
            Assert.Null(tracking.ResolvedAt);
        }
    }

    [Fact]
    public void RejectsSlaPolicyIdThatDoesNotExist()
    {
        using var context = _fixture.CreateContext();

        context.SlaTrackings.Add(new SlaTracking
        {
            Id = Guid.NewGuid(),
            SlaPolicyId = Guid.NewGuid(),
            EntityId = Guid.NewGuid(),
            TargetAt = DateTime.UtcNow.AddHours(24),
        });

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }
}
