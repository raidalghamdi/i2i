using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class ApprovalStepDecisionConfigurationTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public ApprovalStepDecisionConfigurationTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    private static (Guid instanceId, Guid stepId, Guid deciderId, Guid approveTypeId) SeedPrerequisites(InnovationDbContext context, string suffix)
    {
        var chainId = Guid.NewGuid();
        context.ApprovalChains.Add(new ApprovalChain { Id = chainId, Code = $"chain-{suffix}", NameAr = "أ", NameEn = "A", EntityType = "idea" });

        var roleId = Guid.NewGuid();
        context.Roles.Add(new Role { Id = roleId, Code = $"role-{suffix}", NameAr = "دور", NameEn = "Role" });

        var deciderId = Guid.NewGuid();
        context.Users.Add(new User { Id = deciderId, SamAccountName = $"decider-{suffix}", Email = $"decider-{suffix}@gac-demo.sa", FullNameAr = "أ", FullNameEn = "Decider" });

        context.SaveChanges();

        var stepId = Guid.NewGuid();
        context.ApprovalChainSteps.Add(new ApprovalChainStep { Id = stepId, ApprovalChainId = chainId, StepOrder = 1, RoleId = roleId });

        var pendingStatusId = context.ApprovalInstanceStatuses.Single(s => s.Code == "pending").Id;
        var instanceId = Guid.NewGuid();
        context.ApprovalInstances.Add(new ApprovalInstance
        {
            Id = instanceId,
            ApprovalChainId = chainId,
            EntityType = "idea",
            EntityId = Guid.NewGuid(),
            ApprovalInstanceStatusId = pendingStatusId,
        });

        context.SaveChanges();

        var approveTypeId = context.ApprovalDecisionTypes.Single(d => d.Code == "approve").Id;

        return (instanceId, stepId, deciderId, approveTypeId);
    }

    [Fact]
    public void SavesDecisionAndCascadesWhenInstanceIsDeleted()
    {
        Guid instanceId;
        Guid decisionId;

        using (var context = _fixture.CreateContext())
        {
            var (seededInstanceId, stepId, deciderId, approveTypeId) = SeedPrerequisites(context, "t5a");
            instanceId = seededInstanceId;

            var decision = new ApprovalStepDecision
            {
                Id = Guid.NewGuid(),
                ApprovalInstanceId = instanceId,
                ApprovalChainStepId = stepId,
                DeciderId = deciderId,
                ApprovalDecisionTypeId = approveTypeId,
                CommentsEn = "Looks good",
            };
            decisionId = decision.Id;

            context.ApprovalStepDecisions.Add(decision);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var decision = context.ApprovalStepDecisions
                .Include(d => d.ApprovalDecisionType)
                .Single(d => d.Id == decisionId);
            Assert.Equal("approve", decision.ApprovalDecisionType.Code);
            Assert.Null(decision.CommentsAr);

            context.ApprovalInstances.Remove(context.ApprovalInstances.Single(i => i.Id == instanceId));
            context.SaveChanges();

            Assert.False(context.ApprovalStepDecisions.Any(d => d.Id == decisionId));
        }
    }

    [Fact]
    public void AllowsNullCommentsOnADecision()
    {
        using var context = _fixture.CreateContext();
        var (instanceId, stepId, deciderId, approveTypeId) = SeedPrerequisites(context, "t5b");

        context.ApprovalStepDecisions.Add(new ApprovalStepDecision
        {
            Id = Guid.NewGuid(),
            ApprovalInstanceId = instanceId,
            ApprovalChainStepId = stepId,
            DeciderId = deciderId,
            ApprovalDecisionTypeId = approveTypeId,
        });

        var exception = Record.Exception(() => context.SaveChanges());
        Assert.Null(exception);
    }
}
