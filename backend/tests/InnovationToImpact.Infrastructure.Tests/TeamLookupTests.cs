using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class TeamLookupTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public TeamLookupTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void SeedsBothTeamMemberRoleCodes()
    {
        using var context = _fixture.CreateContext();
        var codes = context.TeamMemberRoles.Select(r => r.Code).ToList();

        Assert.Equal(2, codes.Count);
        Assert.Contains("leader", codes);
        Assert.Contains("member", codes);
    }

    [Fact]
    public void SeedsAllFourTeamInvitationStatusCodes()
    {
        using var context = _fixture.CreateContext();
        var codes = context.TeamInvitationStatuses.Select(s => s.Code).ToList();

        Assert.Equal(4, codes.Count);
        Assert.Contains("pending", codes);
        Assert.Contains("accepted", codes);
        Assert.Contains("declined", codes);
        Assert.Contains("expired", codes);
    }

    [Fact]
    public void RejectsDuplicateTeamMemberRoleCode()
    {
        using var context = _fixture.CreateContext();
        context.TeamMemberRoles.Add(new TeamMemberRole { Id = Guid.NewGuid(), Code = "leader", NameAr = "مكرر", NameEn = "Dup", SortOrder = 99 });

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }
}
