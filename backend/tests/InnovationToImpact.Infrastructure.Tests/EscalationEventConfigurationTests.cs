using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class EscalationEventConfigurationTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public EscalationEventConfigurationTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    private static (Guid escalationId, Guid managerTierId, Guid directorTierId, Guid actorId) SeedPrerequisites(InnovationDbContext context, string suffix)
    {
        var managerTierId = context.EscalationTiers.Single(t => t.Code == "manager").Id;
        var directorTierId = context.EscalationTiers.Single(t => t.Code == "director").Id;
        var openStatusId = context.EscalationStatuses.Single(s => s.Code == "open").Id;

        var actorId = Guid.NewGuid();
        context.Users.Add(new User { Id = actorId, SamAccountName = $"actor-{suffix}", Email = $"actor-{suffix}@gac-demo.sa", FullNameAr = "أ", FullNameEn = "Actor" });
        context.SaveChanges();

        var escalationId = Guid.NewGuid();
        context.Escalations.Add(new Escalation
        {
            Id = escalationId,
            EntityType = "idea",
            EntityId = Guid.NewGuid(),
            EscalationTierId = managerTierId,
            ReasonAr = "أ", ReasonEn = "A",
            EscalationStatusId = openStatusId,
        });
        context.SaveChanges();

        return (escalationId, managerTierId, directorTierId, actorId);
    }

    [Fact]
    public void SavesBumpEventWithTierTransition()
    {
        Guid eventId;

        using (var context = _fixture.CreateContext())
        {
            var (escalationId, managerTierId, directorTierId, actorId) = SeedPrerequisites(context, "ee-t5a");

            var escalationEvent = new EscalationEvent
            {
                Id = Guid.NewGuid(),
                EscalationId = escalationId,
                EventType = "bumped",
                FromTierId = managerTierId,
                ToTierId = directorTierId,
                ActorId = actorId,
                Notes = "Escalated after no response",
            };
            eventId = escalationEvent.Id;

            context.EscalationEvents.Add(escalationEvent);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var escalationEvent = context.EscalationEvents
                .Include(e => e.FromTier)
                .Include(e => e.ToTier)
                .Single(e => e.Id == eventId);
            Assert.Equal("manager", escalationEvent.FromTier!.Code);
            Assert.Equal("director", escalationEvent.ToTier!.Code);
            Assert.Equal("bumped", escalationEvent.EventType);
        }
    }

    [Fact]
    public void AllowsNullTiersAndActorForASystemGeneratedEvent()
    {
        using var context = _fixture.CreateContext();
        var (escalationId, _, _, _) = SeedPrerequisites(context, "ee-t5b");

        context.EscalationEvents.Add(new EscalationEvent
        {
            Id = Guid.NewGuid(),
            EscalationId = escalationId,
            EventType = "auto_reminder_sent",
            FromTierId = null,
            ToTierId = null,
            ActorId = null,
        });

        var exception = Record.Exception(() => context.SaveChanges());
        Assert.Null(exception);
    }
}
