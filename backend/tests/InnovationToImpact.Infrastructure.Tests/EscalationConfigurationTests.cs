using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class EscalationConfigurationTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public EscalationConfigurationTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    private static (Guid tierId, Guid statusId) SeedLookups(InnovationDbContext context)
    {
        var tierId = context.EscalationTiers.Single(t => t.Code == "manager").Id;
        var statusId = context.EscalationStatuses.Single(s => s.Code == "open").Id;
        return (tierId, statusId);
    }

    [Fact]
    public void SavesEscalationWithAssignedOwner()
    {
        Guid escalationId;
        Guid ownerId;

        using (var context = _fixture.CreateContext())
        {
            var (tierId, statusId) = SeedLookups(context);

            ownerId = Guid.NewGuid();
            context.Users.Add(new User { Id = ownerId, SamAccountName = "esc-owner-t4a", Email = "esc-owner-t4a@gac-demo.sa", FullNameAr = "أ", FullNameEn = "Owner" });
            context.SaveChanges();

            var escalation = new Escalation
            {
                Id = Guid.NewGuid(),
                EntityType = "idea",
                EntityId = Guid.NewGuid(),
                EscalationTierId = tierId,
                ReasonAr = "سبب",
                ReasonEn = "Reason",
                EscalationStatusId = statusId,
                OwnerId = ownerId,
            };
            escalationId = escalation.Id;

            context.Escalations.Add(escalation);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var escalation = context.Escalations
                .Include(e => e.EscalationTier)
                .Include(e => e.EscalationStatus)
                .Single(e => e.Id == escalationId);
            Assert.Equal("manager", escalation.EscalationTier.Code);
            Assert.Equal("open", escalation.EscalationStatus.Code);
            Assert.Equal(ownerId, escalation.OwnerId);
            Assert.Null(escalation.ResolutionAr);
        }
    }

    [Fact]
    public void AllowsNullOwnerForAnUnassignedEscalation()
    {
        using var context = _fixture.CreateContext();
        var (tierId, statusId) = SeedLookups(context);

        context.Escalations.Add(new Escalation
        {
            Id = Guid.NewGuid(),
            EntityType = "idea",
            EntityId = Guid.NewGuid(),
            EscalationTierId = tierId,
            ReasonAr = "أ", ReasonEn = "A",
            EscalationStatusId = statusId,
            OwnerId = null,
        });

        var exception = Record.Exception(() => context.SaveChanges());
        Assert.Null(exception);
    }
}
