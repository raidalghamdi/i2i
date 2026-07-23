using InnovationToImpact.Domain.Auth;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class ApprovalSeedTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public ApprovalSeedTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Seeds_CommitteePublish_Chain_With_Evaluator_And_Judge_Steps()
    {
        using var ctx = _fixture.CreateContext();

        var committee = await ctx.ApprovalChains.SingleAsync(c => c.Code == "committee-publish");
        Assert.Equal("committee_decision", committee.EntityType);
        Assert.True(committee.IsActive);

        var steps = await ctx.ApprovalChainSteps
            .Where(s => s.ApprovalChainId == committee.Id)
            .OrderBy(s => s.StepOrder)
            .ToListAsync();

        Assert.Equal(2, steps.Count);

        Assert.Equal(1, steps[0].MinApprovers);
        Assert.True(steps[0].IsRequired);
        var step0Role = await ctx.Roles.SingleAsync(r => r.Id == steps[0].RoleId);
        Assert.Equal(RoleCodes.Evaluator, step0Role.Code);

        Assert.Equal(2, steps[1].MinApprovers);
        Assert.True(steps[1].IsRequired);
        var step1Role = await ctx.Roles.SingleAsync(r => r.Id == steps[1].RoleId);
        Assert.Equal(RoleCodes.Judge, step1Role.Code);
    }

    [Fact]
    public async Task Seeds_IdeaApprove_Chain_With_Single_Admin_Step()
    {
        using var ctx = _fixture.CreateContext();

        var idea = await ctx.ApprovalChains.SingleAsync(c => c.Code == "idea-approve");
        Assert.Equal("idea", idea.EntityType);
        Assert.True(idea.IsActive);

        var steps = await ctx.ApprovalChainSteps
            .Where(s => s.ApprovalChainId == idea.Id)
            .ToListAsync();

        Assert.Single(steps);
        Assert.Equal(1, steps[0].MinApprovers);
        Assert.True(steps[0].IsRequired);

        var role = await ctx.Roles.SingleAsync(r => r.Id == steps[0].RoleId);
        Assert.Equal(RoleCodes.Admin, role.Code);
    }
}
