using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class SlaPolicyConfigurationTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public SlaPolicyConfigurationTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void SavesSlaPolicyForAStateTransition()
    {
        Guid policyId;

        using (var context = _fixture.CreateContext())
        {
            var policy = new SlaPolicy
            {
                Id = Guid.NewGuid(),
                EntityType = "idea",
                FromState = "submitted",
                ToState = "screening",
                TargetHours = 48,
                WarnAtPct = 80,
            };
            policyId = policy.Id;

            context.SlaPolicies.Add(policy);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var policy = context.SlaPolicies.Single(p => p.Id == policyId);
            Assert.Equal(48, policy.TargetHours);
            Assert.Equal(80, policy.WarnAtPct);
        }
    }

    [Fact]
    public void RejectsDuplicatePolicyForSameTransition()
    {
        using var context = _fixture.CreateContext();

        context.SlaPolicies.Add(new SlaPolicy { Id = Guid.NewGuid(), EntityType = "idea", FromState = "screening", ToState = "evaluation", TargetHours = 24, WarnAtPct = 75 });
        context.SaveChanges();

        context.SlaPolicies.Add(new SlaPolicy { Id = Guid.NewGuid(), EntityType = "idea", FromState = "screening", ToState = "evaluation", TargetHours = 36, WarnAtPct = 90 });

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }
}
