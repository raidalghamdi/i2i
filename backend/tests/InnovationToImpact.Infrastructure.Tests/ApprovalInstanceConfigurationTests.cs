using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class ApprovalInstanceConfigurationTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public ApprovalInstanceConfigurationTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    private static (Guid chainId, Guid pendingStatusId) SeedPrerequisites(InnovationDbContext context, string suffix)
    {
        var chainId = Guid.NewGuid();
        context.ApprovalChains.Add(new ApprovalChain { Id = chainId, Code = $"chain-{suffix}", NameAr = "أ", NameEn = "A", EntityType = "idea" });
        context.SaveChanges();

        var pendingStatusId = context.ApprovalInstanceStatuses.Single(s => s.Code == "pending").Id;
        return (chainId, pendingStatusId);
    }

    [Fact]
    public void SavesPendingInstanceForATrackedEntity()
    {
        Guid instanceId;
        Guid trackedEntityId;

        using (var context = _fixture.CreateContext())
        {
            var (chainId, pendingStatusId) = SeedPrerequisites(context, "t4a");
            trackedEntityId = Guid.NewGuid();

            var instance = new ApprovalInstance
            {
                Id = Guid.NewGuid(),
                ApprovalChainId = chainId,
                EntityType = "idea",
                EntityId = trackedEntityId,
                ApprovalInstanceStatusId = pendingStatusId,
            };
            instanceId = instance.Id;

            context.ApprovalInstances.Add(instance);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var instance = context.ApprovalInstances
                .Include(i => i.ApprovalInstanceStatus)
                .Single(i => i.Id == instanceId);
            Assert.Equal("pending", instance.ApprovalInstanceStatus.Code);
            Assert.Equal(trackedEntityId, instance.EntityId);
            Assert.Equal(1, instance.CurrentStepOrder);
            Assert.Null(instance.CompletedAt);
        }
    }

    [Fact]
    public void RejectsApprovalChainIdThatDoesNotExist()
    {
        using var context = _fixture.CreateContext();
        var pendingStatusId = context.ApprovalInstanceStatuses.Single(s => s.Code == "pending").Id;

        context.ApprovalInstances.Add(new ApprovalInstance
        {
            Id = Guid.NewGuid(),
            ApprovalChainId = Guid.NewGuid(),
            EntityType = "idea",
            EntityId = Guid.NewGuid(),
            ApprovalInstanceStatusId = pendingStatusId,
        });

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }
}
