using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class TeamConfigurationTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public TeamConfigurationTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void SavesTeamWithRequiredLeader()
    {
        Guid teamId;
        Guid leaderId;

        using (var context = _fixture.CreateContext())
        {
            leaderId = Guid.NewGuid();
            context.Users.Add(new User { Id = leaderId, SamAccountName = "leader-team-t2a", Email = "leader-team-t2a@gac-demo.sa", FullNameAr = "أ", FullNameEn = "Leader" });
            context.SaveChanges();

            var team = new Team
            {
                Id = Guid.NewGuid(),
                NameAr = "فريق",
                NameEn = "Team",
                Slug = "team-t2a",
                LeaderId = leaderId,
            };
            teamId = team.Id;

            context.Teams.Add(team);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var team = context.Teams.Single(t => t.Id == teamId);
            Assert.Equal(leaderId, team.LeaderId);
            Assert.True(team.IsActive);
        }
    }

    [Fact]
    public void RejectsDuplicateSlug()
    {
        using var context = _fixture.CreateContext();
        var leaderId = Guid.NewGuid();
        context.Users.Add(new User { Id = leaderId, SamAccountName = "leader-team-t2b", Email = "leader-team-t2b@gac-demo.sa", FullNameAr = "أ", FullNameEn = "Leader" });
        context.SaveChanges();

        context.Teams.Add(new Team { Id = Guid.NewGuid(), NameAr = "أ", NameEn = "A", Slug = "dup-slug", LeaderId = leaderId });
        context.SaveChanges();

        context.Teams.Add(new Team { Id = Guid.NewGuid(), NameAr = "ب", NameEn = "B", Slug = "dup-slug", LeaderId = leaderId });

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }
}
