using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Roster;
using InnovationToImpact.Infrastructure.Data;
using InnovationToImpact.Infrastructure.Roster;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

// Deliberately NOT IClassFixture<SqliteContextFixture> -- these tests assert absolute
// RoleInvitationReminderResult counts, which only hold against tables no other test method has
// already written rows into, and xUnit does not guarantee [Fact] execution order. A fresh
// fixture per test method isolates each test's tables, the established fix from prior Phase 2
// plans (Audit Hash-Chain, Email Outbox Worker, SLA Scan, Invitation Reminder Processor).
public class RoleInvitationReminderProcessorTests : IDisposable
{
    private readonly SqliteContextFixture _fixture = new();

    private static Guid SeedRoleAndInviter(InnovationDbContext db, out Guid inviterId)
    {
        var roleId = Guid.NewGuid();
        db.Roles.Add(new Role { Id = roleId, Code = "test_roster_role", NameAr = "دور الاختبار", NameEn = "Test Roster Role", SortOrder = 200 });

        var inviter = Guid.NewGuid();
        db.Users.Add(new User { Id = inviter, SamAccountName = "inviter1", Email = "inviter1@gac-demo.sa", FullNameAr = "inviter1", FullNameEn = "inviter1" });
        db.SaveChanges();

        inviterId = inviter;
        return roleId;
    }

    private static Guid SeedInvitation(
        InnovationDbContext db,
        Guid roleId,
        Guid inviterId,
        string statusCode,
        DateTime? deadlineAt,
        int reminderCount = 0,
        DateTime? lastReminderAt = null,
        string samAccountName = "invitee1",
        string? email = "invitee1@gac-demo.sa")
    {
        var status = db.RoleInvitationStatuses.Single(s => s.Code == statusCode);

        var invitation = new RoleInvitation
        {
            Id = Guid.NewGuid(),
            SamAccountName = samAccountName,
            RoleId = roleId,
            Email = email,
            RoleInvitationStatusId = status.Id,
            DeadlineAt = deadlineAt,
            ReminderCount = reminderCount,
            LastReminderAt = lastReminderAt,
            Source = "manual",
            InvitedById = inviterId,
        };
        db.RoleInvitations.Add(invitation);
        db.SaveChanges();
        return invitation.Id;
    }

    private static void UpdateSettings(InnovationDbContext db, bool? enabled = null, int? reminderGapHours = null, int? maxReminders = null)
    {
        var settings = db.RoleInvitationSettings.Single();
        if (enabled.HasValue) settings.Enabled = enabled.Value;
        if (reminderGapHours.HasValue) settings.ReminderGapHours = reminderGapHours.Value;
        if (maxReminders.HasValue) settings.MaxReminders = maxReminders.Value;
        db.SaveChanges();
    }

    [Fact]
    public async Task ProcessAsync_PastDeadline_ExpiresInvitationAndCancelsPendingGrant()
    {
        using var db = _fixture.CreateContext();
        var roleId = SeedRoleAndInviter(db, out var inviterId);
        var id = SeedInvitation(db, roleId, inviterId, RoleInvitationStatusCodes.Pending, DateTime.UtcNow.AddDays(-1));
        db.PendingRoleGrants.Add(new PendingRoleGrant { Id = Guid.NewGuid(), SamAccountName = "invitee1", RoleId = roleId, GrantedById = inviterId });
        db.SaveChanges();

        var processor = new RoleInvitationReminderProcessor(db);
        var result = await processor.ProcessAsync(CancellationToken.None);

        Assert.Equal(new RoleInvitationReminderResult(1, 1, 0), result);

        var updated = await db.RoleInvitations.Include(ri => ri.RoleInvitationStatus).SingleAsync(ri => ri.Id == id);
        Assert.Equal(RoleInvitationStatusCodes.Expired, updated.RoleInvitationStatus.Code);
        Assert.Empty(await db.PendingRoleGrants.Where(g => g.SamAccountName == "invitee1").ToListAsync());
    }

    [Fact]
    public async Task ProcessAsync_WithinGapWindow_QueuesReminderAndIncrementsCount()
    {
        using var db = _fixture.CreateContext();
        var roleId = SeedRoleAndInviter(db, out var inviterId);
        var id = SeedInvitation(db, roleId, inviterId, RoleInvitationStatusCodes.Pending, DateTime.UtcNow.AddDays(10), reminderCount: 0, lastReminderAt: null);

        var before = DateTime.UtcNow;
        var processor = new RoleInvitationReminderProcessor(db);
        var result = await processor.ProcessAsync(CancellationToken.None);
        var after = DateTime.UtcNow;

        Assert.Equal(new RoleInvitationReminderResult(1, 0, 1), result);

        var updated = await db.RoleInvitations.SingleAsync(ri => ri.Id == id);
        Assert.Equal(1, updated.ReminderCount);
        Assert.NotNull(updated.LastReminderAt);
        Assert.InRange(updated.LastReminderAt!.Value, before, after);

        var queuedEmail = await db.EmailOutboxes.SingleAsync(e => e.ToEmail == "invitee1@gac-demo.sa");
        Assert.Equal("role_invitation_reminder", queuedEmail.Category);
    }

    [Fact]
    public async Task ProcessAsync_WithinGapHoursSinceLastReminder_SkipsWithoutQueuing()
    {
        using var db = _fixture.CreateContext();
        UpdateSettings(db, reminderGapHours: 48);
        var roleId = SeedRoleAndInviter(db, out var inviterId);
        var id = SeedInvitation(
            db, roleId, inviterId, RoleInvitationStatusCodes.Pending, DateTime.UtcNow.AddDays(10),
            reminderCount: 1, lastReminderAt: DateTime.UtcNow.AddHours(-1));

        var processor = new RoleInvitationReminderProcessor(db);
        var result = await processor.ProcessAsync(CancellationToken.None);

        Assert.Equal(new RoleInvitationReminderResult(1, 0, 0), result);

        var updated = await db.RoleInvitations.SingleAsync(ri => ri.Id == id);
        Assert.Equal(1, updated.ReminderCount);
        Assert.False(await db.EmailOutboxes.AnyAsync(e => e.ToEmail == "invitee1@gac-demo.sa"));
    }

    [Fact]
    public async Task ProcessAsync_AtMaxReminders_SkipsWithoutQueuing()
    {
        using var db = _fixture.CreateContext();
        UpdateSettings(db, maxReminders: 3);
        var roleId = SeedRoleAndInviter(db, out var inviterId);
        SeedInvitation(
            db, roleId, inviterId, RoleInvitationStatusCodes.Pending, DateTime.UtcNow.AddDays(10),
            reminderCount: 3, lastReminderAt: DateTime.UtcNow.AddDays(-10));

        var processor = new RoleInvitationReminderProcessor(db);
        var result = await processor.ProcessAsync(CancellationToken.None);

        Assert.Equal(new RoleInvitationReminderResult(1, 0, 0), result);
        Assert.False(await db.EmailOutboxes.AnyAsync(e => e.ToEmail == "invitee1@gac-demo.sa"));
    }

    [Fact]
    public async Task ProcessAsync_SettingsDisabled_StillExpiresButSkipsReminders()
    {
        using var db = _fixture.CreateContext();
        UpdateSettings(db, enabled: false);
        var roleId = SeedRoleAndInviter(db, out var inviterId);
        var expiredId = SeedInvitation(db, roleId, inviterId, RoleInvitationStatusCodes.Pending, DateTime.UtcNow.AddDays(-1));
        db.PendingRoleGrants.Add(new PendingRoleGrant { Id = Guid.NewGuid(), SamAccountName = "invitee1", RoleId = roleId, GrantedById = inviterId });
        var stillPendingId = SeedInvitation(db, roleId, inviterId, RoleInvitationStatusCodes.Pending, DateTime.UtcNow.AddDays(10), samAccountName: "invitee2", email: "invitee2@gac-demo.sa");
        db.SaveChanges();

        var processor = new RoleInvitationReminderProcessor(db);
        var result = await processor.ProcessAsync(CancellationToken.None);

        // Expiry (and PendingRoleGrant cancellation) still happens even when reminder
        // emails are disabled -- disabling reminders must not silently reopen the
        // grant-cancellation safety hole described in Decision 2 of the design spec.
        Assert.Equal(new RoleInvitationReminderResult(2, 1, 0), result);
        Assert.False(await db.EmailOutboxes.AnyAsync());

        var expired = await db.RoleInvitations.Include(ri => ri.RoleInvitationStatus).SingleAsync(ri => ri.Id == expiredId);
        Assert.Equal(RoleInvitationStatusCodes.Expired, expired.RoleInvitationStatus.Code);
        Assert.Empty(await db.PendingRoleGrants.Where(g => g.SamAccountName == "invitee1").ToListAsync());

        var stillPending = await db.RoleInvitations.Include(ri => ri.RoleInvitationStatus).SingleAsync(ri => ri.Id == stillPendingId);
        Assert.Equal(RoleInvitationStatusCodes.Pending, stillPending.RoleInvitationStatus.Code);
        Assert.Equal(0, stillPending.ReminderCount);
    }

    [Fact]
    public async Task ProcessAsync_AppliedOrWithdrawnInvitations_AreIgnored()
    {
        using var db = _fixture.CreateContext();
        var roleId = SeedRoleAndInviter(db, out var inviterId);
        SeedInvitation(db, roleId, inviterId, RoleInvitationStatusCodes.Applied, DateTime.UtcNow.AddDays(-1), samAccountName: "applied1", email: "applied1@gac-demo.sa");
        SeedInvitation(db, roleId, inviterId, RoleInvitationStatusCodes.Withdrawn, DateTime.UtcNow.AddDays(-1), samAccountName: "withdrawn1", email: "withdrawn1@gac-demo.sa");
        SeedInvitation(db, roleId, inviterId, RoleInvitationStatusCodes.Expired, DateTime.UtcNow.AddDays(-1), samAccountName: "expired1", email: "expired1@gac-demo.sa");

        var processor = new RoleInvitationReminderProcessor(db);
        var result = await processor.ProcessAsync(CancellationToken.None);

        Assert.Equal(new RoleInvitationReminderResult(0, 0, 0), result);
        Assert.False(await db.EmailOutboxes.AnyAsync());
    }

    public void Dispose() => _fixture.Dispose();
}
