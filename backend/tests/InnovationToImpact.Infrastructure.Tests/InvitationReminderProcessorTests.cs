using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Invitations;
using InnovationToImpact.Infrastructure.Data;
using InnovationToImpact.Infrastructure.Invitations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

// Deliberately NOT IClassFixture<SqliteContextFixture> -- these tests assert absolute
// InvitationReminderResult counts, which only hold against tables no other test method has
// already written rows into, and xUnit does not guarantee [Fact] execution order. A fresh
// fixture per test method isolates each test's tables, the established fix from prior Phase 2
// plans (Audit Hash-Chain, Email Outbox Worker, SLA Scan).
public class InvitationReminderProcessorTests : IDisposable
{
    private readonly SqliteContextFixture _fixture = new();

    private static InvitationReminderOptions NewOptions(int reminderWindowDays = 3) => new() { ReminderWindowDays = reminderWindowDays };

    private static Guid SeedTeam(InnovationDbContext db)
    {
        var leaderId = Guid.NewGuid();
        db.Users.Add(new User { Id = leaderId, SamAccountName = "leader1", Email = "leader1@gac-demo.sa", FullNameAr = "leader1", FullNameEn = "leader1" });
        db.SaveChanges();

        var teamId = Guid.NewGuid();
        db.Teams.Add(new Team { Id = teamId, NameAr = "فريق الاختبار", NameEn = "Test Team", Slug = "test-team", LeaderId = leaderId });
        db.SaveChanges();

        return teamId;
    }

    private static Guid SeedInvitation(InnovationDbContext db, Guid teamId, string statusCode, DateTime expiresAt)
    {
        var status = db.TeamInvitationStatuses.Single(s => s.Code == statusCode);
        var leaderId = db.Teams.Single(t => t.Id == teamId).LeaderId;

        var invitation = new TeamInvitation
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            InvitedEmail = "invitee@gac-demo.sa",
            InvitedById = leaderId,
            Token = Guid.NewGuid().ToString("N"),
            TeamInvitationStatusId = status.Id,
            ExpiresAt = expiresAt,
        };
        db.TeamInvitations.Add(invitation);
        db.SaveChanges();
        return invitation.Id;
    }

    [Fact]
    public async Task InvitationPastExpiresAt_TransitionsToExpired_NoReminderQueued()
    {
        using var db = _fixture.CreateContext();
        var teamId = SeedTeam(db);
        var id = SeedInvitation(db, teamId, TeamInvitationStatusCodes.Pending, DateTime.UtcNow.AddDays(-1));

        var processor = new InvitationReminderProcessor(db, Options.Create(NewOptions()));
        var result = await processor.ProcessAsync(CancellationToken.None);

        Assert.Equal(new InvitationReminderResult(1, 1, 0), result);

        var updated = await db.TeamInvitations.Include(i => i.TeamInvitationStatus).SingleAsync(i => i.Id == id);
        Assert.Equal(TeamInvitationStatusCodes.Expired, updated.TeamInvitationStatus.Code);
        Assert.False(await db.EmailOutboxes.AnyAsync(e => e.ToEmail == "invitee@gac-demo.sa"));
    }

    [Fact]
    public async Task InvitationWithinReminderWindow_QueuesReminderEmail_NoStatusChange()
    {
        using var db = _fixture.CreateContext();
        var teamId = SeedTeam(db);
        var id = SeedInvitation(db, teamId, TeamInvitationStatusCodes.Pending, DateTime.UtcNow.AddDays(2));

        var processor = new InvitationReminderProcessor(db, Options.Create(NewOptions(reminderWindowDays: 3)));
        var result = await processor.ProcessAsync(CancellationToken.None);

        Assert.Equal(new InvitationReminderResult(1, 0, 1), result);

        var updated = await db.TeamInvitations.Include(i => i.TeamInvitationStatus).SingleAsync(i => i.Id == id);
        Assert.Equal(TeamInvitationStatusCodes.Pending, updated.TeamInvitationStatus.Code);

        var queuedEmail = await db.EmailOutboxes.SingleAsync(e => e.ToEmail == "invitee@gac-demo.sa");
        Assert.Equal("team_invitation_reminder", queuedEmail.Category);
    }

    [Fact]
    public async Task InvitationOutsideReminderWindow_NoAction()
    {
        using var db = _fixture.CreateContext();
        var teamId = SeedTeam(db);
        SeedInvitation(db, teamId, TeamInvitationStatusCodes.Pending, DateTime.UtcNow.AddDays(10));

        var processor = new InvitationReminderProcessor(db, Options.Create(NewOptions(reminderWindowDays: 3)));
        var result = await processor.ProcessAsync(CancellationToken.None);

        Assert.Equal(new InvitationReminderResult(1, 0, 0), result);
        Assert.False(await db.EmailOutboxes.AnyAsync(e => e.ToEmail == "invitee@gac-demo.sa"));
    }

    [Fact]
    public async Task NonPendingInvitation_IsExcludedEntirely_EvenIfPastExpiresAt()
    {
        using var db = _fixture.CreateContext();
        var teamId = SeedTeam(db);
        SeedInvitation(db, teamId, TeamInvitationStatusCodes.Accepted, DateTime.UtcNow.AddDays(-1));

        var processor = new InvitationReminderProcessor(db, Options.Create(NewOptions()));
        var result = await processor.ProcessAsync(CancellationToken.None);

        Assert.Equal(new InvitationReminderResult(0, 0, 0), result);
    }

    public void Dispose() => _fixture.Dispose();
}
