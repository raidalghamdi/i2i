using InnovationToImpact.Domain.Auth;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Invitations;
using InnovationToImpact.Infrastructure.Briefing;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

// Deliberately NOT IClassFixture<SqliteContextFixture> -- these tests assert absolute
// WeeklyBriefingResult counts, which only hold against tables no other test method has
// already written rows into, and xUnit does not guarantee [Fact] execution order. A fresh
// fixture per test method isolates each test's tables, the established fix from prior Phase 2
// plans (Audit Hash-Chain, Email Outbox Worker, SLA Scan, Invitation Reminders).
public class WeeklyBriefingProcessorTests : IDisposable
{
    private readonly SqliteContextFixture _fixture = new();

    private static WeeklyBriefingOptions NewOptions(int windowDays = 7) => new() { WindowDays = windowDays };

    private static Guid SeedUserWithRole(InnovationDbContext db, string samAccountName, string email, string roleCode)
    {
        var roleId = db.Roles.Single(r => r.Code == roleCode).Id;
        var userId = Guid.NewGuid();
        db.Users.Add(new User { Id = userId, SamAccountName = samAccountName, Email = email, FullNameAr = samAccountName, FullNameEn = samAccountName });
        db.SaveChanges();
        db.Set<UserRole>().Add(new UserRole { UserId = userId, RoleId = roleId, IsPrimary = true });
        db.SaveChanges();
        return userId;
    }

    private static Guid SeedTeam(InnovationDbContext db, Guid leaderId)
    {
        var teamId = Guid.NewGuid();
        db.Teams.Add(new Team { Id = teamId, NameAr = "فريق", NameEn = "Team", Slug = $"team-{Guid.NewGuid():N}", LeaderId = leaderId });
        db.SaveChanges();
        return teamId;
    }

    private static void SeedInvitation(InnovationDbContext db, Guid teamId, Guid inviterId, string statusCode, DateTime expiresAt, DateTime? acceptedAt = null)
    {
        var statusId = db.TeamInvitationStatuses.Single(s => s.Code == statusCode).Id;
        db.TeamInvitations.Add(new TeamInvitation
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            InvitedEmail = "invitee@gac-demo.sa",
            InvitedById = inviterId,
            Token = Guid.NewGuid().ToString("N"),
            TeamInvitationStatusId = statusId,
            ExpiresAt = expiresAt,
            AcceptedAt = acceptedAt,
        });
        db.SaveChanges();
    }

    private static void SeedSlaBreach(InnovationDbContext db, DateTime breachedAt)
    {
        var policyId = Guid.NewGuid();
        var stateSuffix = Guid.NewGuid().ToString("N");
        db.SlaPolicies.Add(new SlaPolicy { Id = policyId, EntityType = "idea", FromState = $"a-{stateSuffix}", ToState = $"b-{stateSuffix}", TargetHours = 24, WarnAtPct = 80 });
        db.SaveChanges();
        db.SlaTrackings.Add(new SlaTracking
        {
            Id = Guid.NewGuid(),
            SlaPolicyId = policyId,
            EntityId = Guid.NewGuid(),
            OpenedAt = breachedAt.AddHours(-24),
            TargetAt = breachedAt,
            BreachedAt = breachedAt,
        });
        db.SaveChanges();
    }

    private static void SeedAuditEntry(InnovationDbContext db, DateTime occurredAt)
    {
        db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            ChainSeq = db.AuditLogs.Count() + 1,
            RowHash = new string('a', 64),
            EntityType = "idea",
            EntityId = Guid.NewGuid(),
            Action = "create",
            OccurredAt = occurredAt,
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task ComputesAllCounts_AndQueuesOneEmailPerAdmin()
    {
        using var db = _fixture.CreateContext();
        var admin1 = SeedUserWithRole(db, "admin1", "admin1@gac-demo.sa", RoleCodes.Admin);
        var admin2 = SeedUserWithRole(db, "admin2", "admin2@gac-demo.sa", RoleCodes.Admin);
        SeedUserWithRole(db, "submitter1", "submitter1@gac-demo.sa", RoleCodes.Submitter);
        var teamId = SeedTeam(db, admin1);

        SeedSlaBreach(db, DateTime.UtcNow.AddDays(-2));
        SeedSlaBreach(db, DateTime.UtcNow.AddDays(-10)); // outside window

        SeedInvitation(db, teamId, admin1, TeamInvitationStatusCodes.Accepted, DateTime.UtcNow.AddDays(20), acceptedAt: DateTime.UtcNow.AddDays(-1));
        SeedInvitation(db, teamId, admin1, TeamInvitationStatusCodes.Accepted, DateTime.UtcNow.AddDays(20), acceptedAt: DateTime.UtcNow.AddDays(-10)); // outside window
        SeedInvitation(db, teamId, admin1, TeamInvitationStatusCodes.Pending, DateTime.UtcNow.AddDays(5));
        SeedInvitation(db, teamId, admin1, TeamInvitationStatusCodes.Expired, DateTime.UtcNow.AddDays(-3));

        SeedAuditEntry(db, DateTime.UtcNow.AddDays(-1));
        SeedAuditEntry(db, DateTime.UtcNow.AddDays(-15)); // outside window

        var processor = new WeeklyBriefingProcessor(db, Options.Create(NewOptions()));
        var result = await processor.GenerateAsync(CancellationToken.None);

        Assert.Equal(1, result.SlaBreachesThisWeek);
        Assert.Equal(1, result.InvitationsAcceptedThisWeek);
        Assert.Equal(1, result.PendingInvitations);
        Assert.Equal(1, result.ExpiredInvitations);
        Assert.Equal(1, result.AuditEntriesThisWeek);
        Assert.Equal(2, result.RecipientsQueued);

        var queuedEmails = await db.EmailOutboxes.Where(e => e.Category == "weekly_briefing").ToListAsync();
        Assert.Equal(2, queuedEmails.Count);
        Assert.Contains(queuedEmails, e => e.ToEmail == "admin1@gac-demo.sa");
        Assert.Contains(queuedEmails, e => e.ToEmail == "admin2@gac-demo.sa");
        Assert.DoesNotContain(queuedEmails, e => e.ToEmail == "submitter1@gac-demo.sa");
    }

    [Fact]
    public async Task NoAdmins_QueuesNoEmails_StillComputesCounts()
    {
        using var db = _fixture.CreateContext();
        SeedSlaBreach(db, DateTime.UtcNow.AddDays(-1));

        var processor = new WeeklyBriefingProcessor(db, Options.Create(NewOptions()));
        var result = await processor.GenerateAsync(CancellationToken.None);

        Assert.Equal(1, result.SlaBreachesThisWeek);
        Assert.Equal(0, result.RecipientsQueued);
        Assert.False(await db.EmailOutboxes.AnyAsync());
    }

    public void Dispose() => _fixture.Dispose();
}
