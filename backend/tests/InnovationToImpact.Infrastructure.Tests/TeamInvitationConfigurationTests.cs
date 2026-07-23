using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class TeamInvitationConfigurationTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public TeamInvitationConfigurationTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    private static (Guid teamId, Guid inviterId, Guid pendingStatusId) SeedPrerequisites(InnovationDbContext context, string suffix)
    {
        var inviterId = Guid.NewGuid();
        context.Users.Add(new User { Id = inviterId, SamAccountName = $"inviter-{suffix}", Email = $"inviter-{suffix}@gac-demo.sa", FullNameAr = "أ", FullNameEn = "Inviter" });
        context.SaveChanges();

        var teamId = Guid.NewGuid();
        context.Teams.Add(new Team { Id = teamId, NameAr = "فريق", NameEn = "Team", Slug = $"team-{suffix}", LeaderId = inviterId });
        context.SaveChanges();

        var pendingStatusId = context.TeamInvitationStatuses.Single(s => s.Code == "pending").Id;
        return (teamId, inviterId, pendingStatusId);
    }

    [Fact]
    public void SavesTeamInvitationWithDefaultExpiry()
    {
        Guid invitationId;
        DateTime beforeCreate;

        using (var context = _fixture.CreateContext())
        {
            var (teamId, inviterId, pendingStatusId) = SeedPrerequisites(context, "inv-t4a");

            beforeCreate = DateTime.UtcNow;
            var invitation = new TeamInvitation
            {
                Id = Guid.NewGuid(),
                TeamId = teamId,
                InvitedEmail = "invitee@gac-demo.sa",
                InvitedById = inviterId,
                Token = "token-t4a",
                TeamInvitationStatusId = pendingStatusId,
            };
            invitationId = invitation.Id;

            context.TeamInvitations.Add(invitation);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var invitation = context.TeamInvitations
                .Include(i => i.TeamInvitationStatus)
                .Single(i => i.Id == invitationId);
            Assert.Equal("pending", invitation.TeamInvitationStatus.Code);
            Assert.Null(invitation.AcceptedAt);
            Assert.True(invitation.ExpiresAt > beforeCreate.AddDays(13));
            Assert.True(invitation.ExpiresAt < beforeCreate.AddDays(15));
        }
    }

    [Fact]
    public void RejectsDuplicateToken()
    {
        using var context = _fixture.CreateContext();
        var (teamId, inviterId, pendingStatusId) = SeedPrerequisites(context, "inv-t4b");

        context.TeamInvitations.Add(new TeamInvitation
        {
            Id = Guid.NewGuid(), TeamId = teamId, InvitedEmail = "a@gac-demo.sa", InvitedById = inviterId,
            Token = "dup-token", TeamInvitationStatusId = pendingStatusId,
        });
        context.SaveChanges();

        context.TeamInvitations.Add(new TeamInvitation
        {
            Id = Guid.NewGuid(), TeamId = teamId, InvitedEmail = "b@gac-demo.sa", InvitedById = inviterId,
            Token = "dup-token", TeamInvitationStatusId = pendingStatusId,
        });

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }
}
