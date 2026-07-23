using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class TeamMemberConfigurationTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public TeamMemberConfigurationTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    private static (Guid teamId, Guid memberId, Guid memberRoleId) SeedPrerequisites(InnovationDbContext context, string suffix)
    {
        var leaderId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        context.Users.Add(new User { Id = leaderId, SamAccountName = $"leader-{suffix}", Email = $"leader-{suffix}@gac-demo.sa", FullNameAr = "أ", FullNameEn = "Leader" });
        context.Users.Add(new User { Id = memberId, SamAccountName = $"member-{suffix}", Email = $"member-{suffix}@gac-demo.sa", FullNameAr = "ب", FullNameEn = "Member" });
        context.SaveChanges();

        var teamId = Guid.NewGuid();
        context.Teams.Add(new Team { Id = teamId, NameAr = "فريق", NameEn = "Team", Slug = $"team-{suffix}", LeaderId = leaderId });
        context.SaveChanges();

        var memberRoleId = context.TeamMemberRoles.Single(r => r.Code == "member").Id;
        return (teamId, memberId, memberRoleId);
    }

    [Fact]
    public void SavesTeamMemberWithRole()
    {
        Guid teamMemberId;

        using (var context = _fixture.CreateContext())
        {
            var (teamId, memberId, memberRoleId) = SeedPrerequisites(context, "tm-t3a");

            var teamMember = new TeamMember
            {
                Id = Guid.NewGuid(),
                TeamId = teamId,
                UserId = memberId,
                TeamMemberRoleId = memberRoleId,
            };
            teamMemberId = teamMember.Id;

            context.TeamMembers.Add(teamMember);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var teamMember = context.TeamMembers
                .Include(m => m.TeamMemberRole)
                .Single(m => m.Id == teamMemberId);
            Assert.Equal("member", teamMember.TeamMemberRole.Code);
        }
    }

    [Fact]
    public void RejectsDuplicateTeamMemberPair()
    {
        using var context = _fixture.CreateContext();
        var (teamId, memberId, memberRoleId) = SeedPrerequisites(context, "tm-t3b");

        context.TeamMembers.Add(new TeamMember { Id = Guid.NewGuid(), TeamId = teamId, UserId = memberId, TeamMemberRoleId = memberRoleId });
        context.SaveChanges();

        context.TeamMembers.Add(new TeamMember { Id = Guid.NewGuid(), TeamId = teamId, UserId = memberId, TeamMemberRoleId = memberRoleId });

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }
}
