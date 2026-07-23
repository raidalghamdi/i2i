using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class ApprovalChainStepConfigurationTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public ApprovalChainStepConfigurationTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    private static (Guid chainId, Guid roleId) SeedPrerequisites(InnovationDbContext context, string suffix)
    {
        var chainId = Guid.NewGuid();
        context.ApprovalChains.Add(new ApprovalChain { Id = chainId, Code = $"chain-{suffix}", NameAr = "أ", NameEn = "A", EntityType = "idea" });

        var roleId = Guid.NewGuid();
        context.Roles.Add(new Role { Id = roleId, Code = $"role-{suffix}", NameAr = "دور", NameEn = "Role" });

        context.SaveChanges();
        return (chainId, roleId);
    }

    [Fact]
    public void SavesStepAndCascadesWhenChainIsDeleted()
    {
        Guid chainId;
        Guid stepId;

        using (var context = _fixture.CreateContext())
        {
            var (seededChainId, roleId) = SeedPrerequisites(context, "t3a");
            chainId = seededChainId;

            var step = new ApprovalChainStep
            {
                Id = Guid.NewGuid(),
                ApprovalChainId = chainId,
                StepOrder = 1,
                RoleId = roleId,
            };
            stepId = step.Id;

            context.ApprovalChainSteps.Add(step);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var step = context.ApprovalChainSteps.Include(s => s.Role).Single(s => s.Id == stepId);
            Assert.Equal(1, step.StepOrder);
            Assert.True(step.IsRequired);

            context.ApprovalChains.Remove(context.ApprovalChains.Single(c => c.Id == chainId));
            context.SaveChanges();

            Assert.False(context.ApprovalChainSteps.Any(s => s.Id == stepId));
        }
    }

    [Fact]
    public void RejectsDuplicateStepOrderWithinSameChain()
    {
        using var context = _fixture.CreateContext();
        var (chainId, roleId) = SeedPrerequisites(context, "t3b");

        context.ApprovalChainSteps.Add(new ApprovalChainStep { Id = Guid.NewGuid(), ApprovalChainId = chainId, StepOrder = 1, RoleId = roleId });
        context.SaveChanges();

        context.ApprovalChainSteps.Add(new ApprovalChainStep { Id = Guid.NewGuid(), ApprovalChainId = chainId, StepOrder = 1, RoleId = roleId });

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }
}
