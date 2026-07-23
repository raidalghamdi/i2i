using InnovationToImpact.Domain.Audit;
using InnovationToImpact.Domain.Auth;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Roster;
using InnovationToImpact.Infrastructure.Auth;
using InnovationToImpact.Infrastructure.Data;
using InnovationToImpact.Infrastructure.Roster;
using InnovationToImpact.Infrastructure.UserManagement;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class RosterServiceTests
{
    private sealed class FakeAuditLogWriter : IAuditLogWriter
    {
        public List<(string EntityType, Guid EntityId, string Action, Guid? ActorId, string? Payload)> Calls { get; } = new();

        public Task<AuditLog> AppendAsync(string entityType, Guid entityId, string action, Guid? actorId, string? payload, CancellationToken cancellationToken = default)
        {
            Calls.Add((entityType, entityId, action, actorId, payload));
            return Task.FromResult(new AuditLog { Id = Guid.NewGuid(), EntityType = entityType, EntityId = entityId, Action = action, ActorId = actorId, Payload = payload });
        }
    }

    private static RosterService CreateService(InnovationDbContext db, FakeAdIdentityLookupService lookup, FakeAuditLogWriter? auditLog = null)
    {
        var userManagementService = new UserManagementService(db, lookup);
        return new RosterService(db, userManagementService, lookup, auditLog ?? new FakeAuditLogWriter());
    }

    private static Guid SeedUser(SqliteContextFixture fixture, string samAccountName)
    {
        using var db = fixture.CreateContext();
        var id = Guid.NewGuid();
        db.Users.Add(new User { Id = id, SamAccountName = samAccountName, Email = $"{samAccountName}@gac-demo.sa", FullNameAr = samAccountName, FullNameEn = samAccountName });
        db.SaveChanges();
        return id;
    }

    private static void SeedUserRole(SqliteContextFixture fixture, Guid userId, string roleCode)
    {
        using var db = fixture.CreateContext();
        var roleId = db.Roles.Single(r => r.Code == roleCode).Id;
        db.Set<UserRole>().Add(new UserRole { UserId = userId, RoleId = roleId, IsPrimary = false });
        db.SaveChanges();
    }

    private static RoleInvitation SeedRoleInvitation(
        SqliteContextFixture fixture,
        string samAccountName,
        string roleCode,
        string statusCode,
        Guid invitedById,
        string? email = null,
        DateTime? deadlineAt = null,
        int reminderCount = 0,
        DateTime? lastReminderAt = null)
    {
        using var db = fixture.CreateContext();
        var roleId = db.Roles.Single(r => r.Code == roleCode).Id;
        var statusId = db.RoleInvitationStatuses.Single(s => s.Code == statusCode).Id;
        var invitation = new RoleInvitation
        {
            Id = Guid.NewGuid(),
            SamAccountName = samAccountName,
            RoleId = roleId,
            DisplayName = samAccountName,
            Email = email ?? $"{samAccountName}@gac-demo.sa",
            RoleInvitationStatusId = statusId,
            DeadlineAt = deadlineAt,
            RespondedAt = statusCode is RoleInvitationStatusCodes.Applied or RoleInvitationStatusCodes.Withdrawn ? DateTime.UtcNow : null,
            ReminderCount = reminderCount,
            LastReminderAt = lastReminderAt,
            Source = "manual",
            InvitedById = invitedById,
        };
        db.RoleInvitations.Add(invitation);
        db.SaveChanges();
        return invitation;
    }

    private static PendingRoleGrant SeedPendingRoleGrant(SqliteContextFixture fixture, string samAccountName, string roleCode, Guid grantedById)
    {
        using var db = fixture.CreateContext();
        var roleId = db.Roles.Single(r => r.Code == roleCode).Id;
        var grant = new PendingRoleGrant { Id = Guid.NewGuid(), SamAccountName = samAccountName, RoleId = roleId, GrantedById = grantedById };
        db.PendingRoleGrants.Add(grant);
        db.SaveChanges();
        return grant;
    }

    [Fact]
    public async Task GetHubAsync_ReturnsOneRowPerSeededRole_WithCorrectCounts()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUser(fixture, "hubadmin1");

        var evalUser1 = SeedUser(fixture, "hubeval1");
        var evalUser2 = SeedUser(fixture, "hubeval2");
        SeedUserRole(fixture, evalUser1, "evaluator");
        SeedUserRole(fixture, evalUser2, "evaluator");
        SeedRoleInvitation(fixture, "hubpending1", "evaluator", RoleInvitationStatusCodes.Pending, actorId);
        SeedRoleInvitation(fixture, "hubexpired1", "evaluator", RoleInvitationStatusCodes.Expired, actorId);
        SeedRoleInvitation(fixture, "hubwithdrawn1", "evaluator", RoleInvitationStatusCodes.Withdrawn, actorId);

        var judgeUser1 = SeedUser(fixture, "hubjudge1");
        SeedUserRole(fixture, judgeUser1, "judge");

        using var db = fixture.CreateContext();
        var service = CreateService(db, new FakeAdIdentityLookupService(Array.Empty<AdIdentity>()));

        var rows = await service.GetHubAsync();

        Assert.Equal(8, rows.Count);

        var evaluatorRow = rows.Single(r => r.RoleCode == "evaluator");
        Assert.Equal(2, evaluatorRow.ActiveCount);
        Assert.Equal(1, evaluatorRow.PendingCount);
        Assert.Equal(1, evaluatorRow.ExpiredCount);
        Assert.Equal(1, evaluatorRow.WithdrawnCount);

        var judgeRow = rows.Single(r => r.RoleCode == "judge");
        Assert.Equal(1, judgeRow.ActiveCount);
        Assert.Equal(0, judgeRow.PendingCount);
        Assert.Equal(0, judgeRow.ExpiredCount);
        Assert.Equal(0, judgeRow.WithdrawnCount);

        var adminRow = rows.Single(r => r.RoleCode == "admin");
        Assert.Equal(0, adminRow.ActiveCount);
        Assert.Equal(0, adminRow.PendingCount);
        Assert.Equal(0, adminRow.ExpiredCount);
        Assert.Equal(0, adminRow.WithdrawnCount);
    }

    [Fact]
    public async Task CreateInvitationAsync_NewAdUser_CreatesPendingInvitation()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUser(fixture, "createadmin1");
        var lookup = new FakeAdIdentityLookupService(new[] { new AdIdentity("newhire1", "New Hire One", "newhire1@gac-demo.sa", "Innovation", "Engineer", null) });
        var auditLog = new FakeAuditLogWriter();
        using var db = fixture.CreateContext();
        var service = CreateService(db, lookup, auditLog);

        var result = await service.CreateInvitationAsync(new RoleInvitationCreateInput("newhire1", "evaluator", null, "manual"), actorId);

        Assert.Equal(RoleInvitationCommandStatus.Success, result.Status);
        Assert.NotNull(result.Entity);

        using var verifyDb = fixture.CreateContext();
        var settings = verifyDb.RoleInvitationSettings.Single();
        var pendingStatusId = verifyDb.RoleInvitationStatuses.Single(s => s.Code == RoleInvitationStatusCodes.Pending).Id;
        var invitation = verifyDb.RoleInvitations.Single(ri => ri.SamAccountName == "newhire1");
        Assert.Equal(pendingStatusId, invitation.RoleInvitationStatusId);
        Assert.NotNull(invitation.DeadlineAt);
        Assert.True(Math.Abs((invitation.DeadlineAt!.Value - DateTime.UtcNow.AddDays(settings.DefaultExpiresDays)).TotalMinutes) < 5);
        Assert.Equal("New Hire One", invitation.DisplayName);
        Assert.Equal("newhire1@gac-demo.sa", invitation.Email);
        Assert.Null(invitation.RespondedAt);

        Assert.Contains(auditLog.Calls, c => c.Action == "roleInvitation.created" && c.EntityId == invitation.Id);
    }

    [Fact]
    public async Task CreateInvitationAsync_ExistingUserWithoutRole_CreatesAppliedInvitationImmediately()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUser(fixture, "createadmin2");
        SeedUser(fixture, "existinguser2");
        var lookup = new FakeAdIdentityLookupService(new[] { new AdIdentity("existinguser2", "Existing User Two", "existinguser2@gac-demo.sa", null, null, null) });
        using var db = fixture.CreateContext();
        var service = CreateService(db, lookup);

        var result = await service.CreateInvitationAsync(new RoleInvitationCreateInput("existinguser2", "evaluator", null, "manual"), actorId);

        Assert.Equal(RoleInvitationCommandStatus.Success, result.Status);

        using var verifyDb = fixture.CreateContext();
        var appliedStatusId = verifyDb.RoleInvitationStatuses.Single(s => s.Code == RoleInvitationStatusCodes.Applied).Id;
        var invitation = verifyDb.RoleInvitations.Single(ri => ri.SamAccountName == "existinguser2");
        Assert.Equal(appliedStatusId, invitation.RoleInvitationStatusId);
        Assert.NotNull(invitation.RespondedAt);
        Assert.Null(invitation.DeadlineAt);

        var evaluatorRoleId = verifyDb.Roles.Single(r => r.Code == "evaluator").Id;
        var userId = verifyDb.Users.Single(u => u.SamAccountName == "existinguser2").Id;
        Assert.Single(verifyDb.Set<UserRole>().Where(ur => ur.UserId == userId && ur.RoleId == evaluatorRoleId));
    }

    [Fact]
    public async Task CreateInvitationAsync_UserAlreadyHasRole_ReturnsAlreadyApplied_NoRowCreated()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUser(fixture, "createadmin3");
        var userId = SeedUser(fixture, "hasrole1");
        SeedUserRole(fixture, userId, "evaluator");
        var lookup = new FakeAdIdentityLookupService(new[] { new AdIdentity("hasrole1", "Has Role One", "hasrole1@gac-demo.sa", null, null, null) });
        using var db = fixture.CreateContext();
        var service = CreateService(db, lookup);

        var result = await service.CreateInvitationAsync(new RoleInvitationCreateInput("hasrole1", "evaluator", null, "manual"), actorId);

        Assert.Equal(RoleInvitationCommandStatus.AlreadyApplied, result.Status);

        using var verifyDb = fixture.CreateContext();
        Assert.Empty(verifyDb.RoleInvitations.Where(ri => ri.SamAccountName == "hasrole1"));
    }

    [Fact]
    public async Task CreateInvitationAsync_AlreadyPendingRoleInvitationExists_ReturnsAlreadyPending_NoDuplicateRowCreated()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUser(fixture, "createadmin4");
        SeedRoleInvitation(fixture, "alreadypend1", "evaluator", RoleInvitationStatusCodes.Pending, actorId);

        var lookup = new FakeAdIdentityLookupService(new[] { new AdIdentity("alreadypend1", "Already Pending One", "alreadypend1@gac-demo.sa", null, null, null) });
        using var db = fixture.CreateContext();
        var service = CreateService(db, lookup);

        var result = await service.CreateInvitationAsync(new RoleInvitationCreateInput("alreadypend1", "evaluator", null, "manual"), actorId);

        Assert.Equal(RoleInvitationCommandStatus.AlreadyPending, result.Status);
        Assert.Equal(0, lookup.CallCount);

        using var verifyDb = fixture.CreateContext();
        Assert.Single(verifyDb.RoleInvitations.Where(ri => ri.SamAccountName == "alreadypend1"));
    }

    [Fact]
    public async Task CreateInvitationAsync_PendingGrantExistsWithoutRoleInvitation_AdoptsIt()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUser(fixture, "createadmin5");
        SeedPendingRoleGrant(fixture, "grpmember1", "evaluator", actorId);

        var lookup = new FakeAdIdentityLookupService(new[] { new AdIdentity("grpmember1", "Group Member One", "grpmember1@gac-demo.sa", null, null, null) });
        using var db = fixture.CreateContext();
        var service = CreateService(db, lookup);

        var result = await service.CreateInvitationAsync(new RoleInvitationCreateInput("grpmember1", "evaluator", null, "manual"), actorId);

        Assert.Equal(RoleInvitationCommandStatus.Success, result.Status);

        using var verifyDb = fixture.CreateContext();
        var pendingStatusId = verifyDb.RoleInvitationStatuses.Single(s => s.Code == RoleInvitationStatusCodes.Pending).Id;
        var invitation = Assert.Single(verifyDb.RoleInvitations.Where(ri => ri.SamAccountName == "grpmember1"));
        Assert.Equal(pendingStatusId, invitation.RoleInvitationStatusId);

        var evaluatorRoleId = verifyDb.Roles.Single(r => r.Code == "evaluator").Id;
        Assert.Single(verifyDb.PendingRoleGrants.Where(g => g.SamAccountName == "grpmember1" && g.RoleId == evaluatorRoleId));
    }

    [Fact]
    public async Task CreateInvitationAsync_AdUserNotFound_ReturnsError_NoRowCreated()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUser(fixture, "createadmin6");
        var lookup = new FakeAdIdentityLookupService(Array.Empty<AdIdentity>());
        using var db = fixture.CreateContext();
        var service = CreateService(db, lookup);

        var result = await service.CreateInvitationAsync(new RoleInvitationCreateInput("ghost1", "evaluator", null, "manual"), actorId);

        Assert.Equal(RoleInvitationCommandStatus.AdUserNotFound, result.Status);

        using var verifyDb = fixture.CreateContext();
        Assert.Empty(verifyDb.RoleInvitations.Where(ri => ri.SamAccountName == "ghost1"));
    }

    [Fact]
    public async Task CreateInvitationAsync_UnknownRoleCode_ReturnsRoleNotFound()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUser(fixture, "createadmin7");
        var lookup = new FakeAdIdentityLookupService(new[] { new AdIdentity("someuser7", "Some User Seven", "someuser7@gac-demo.sa", null, null, null) });
        using var db = fixture.CreateContext();
        var service = CreateService(db, lookup);

        var result = await service.CreateInvitationAsync(new RoleInvitationCreateInput("someuser7", "not_a_real_role", null, "manual"), actorId);

        Assert.Equal(RoleInvitationCommandStatus.RoleNotFound, result.Status);
    }

    [Fact]
    public async Task BulkCreateInvitationsAsync_MixedValidAndInvalidRows_ReturnsPerRowResults()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUser(fixture, "bulkadmin1");
        SeedRoleInvitation(fixture, "bulkdupe1", "evaluator", RoleInvitationStatusCodes.Pending, actorId);

        var lookup = new FakeAdIdentityLookupService(new[]
        {
            new AdIdentity("bulknew1", "Bulk New One", "bulknew1@gac-demo.sa", null, null, null),
            new AdIdentity("bulkdupe1", "Bulk Dupe One", "bulkdupe1@gac-demo.sa", null, null, null),
        });
        using var db = fixture.CreateContext();
        var service = CreateService(db, lookup);

        var inputs = new[]
        {
            new RoleInvitationCreateInput("bulknew1", "evaluator", null, "import"),
            new RoleInvitationCreateInput("bulkdupe1", "evaluator", null, "import"),
            new RoleInvitationCreateInput("bulkbadrole1", "not_a_real_role", null, "import"),
        };

        var results = await service.BulkCreateInvitationsAsync(inputs, actorId);

        Assert.Equal(3, results.Count);
        Assert.Equal(RoleInvitationCommandStatus.Success, results[0].Status);
        Assert.Equal(RoleInvitationCommandStatus.AlreadyPending, results[1].Status);
        Assert.Equal(RoleInvitationCommandStatus.RoleNotFound, results[2].Status);

        using var verifyDb = fixture.CreateContext();
        Assert.Single(verifyDb.RoleInvitations.Where(ri => ri.SamAccountName == "bulknew1"));
        Assert.Single(verifyDb.RoleInvitations.Where(ri => ri.SamAccountName == "bulkdupe1"));
        Assert.Empty(verifyDb.RoleInvitations.Where(ri => ri.SamAccountName == "bulkbadrole1"));
    }

    [Fact]
    public async Task WithdrawAsync_PendingInvitation_SetsWithdrawnAndDeletesPendingGrant()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUser(fixture, "withdrawadmin1");
        var invitation = SeedRoleInvitation(fixture, "withdrawuser1", "evaluator", RoleInvitationStatusCodes.Pending, actorId);
        SeedPendingRoleGrant(fixture, "withdrawuser1", "evaluator", actorId);

        var lookup = new FakeAdIdentityLookupService(Array.Empty<AdIdentity>());
        var auditLog = new FakeAuditLogWriter();
        using var db = fixture.CreateContext();
        var service = CreateService(db, lookup, auditLog);

        var result = await service.WithdrawAsync(invitation.Id, actorId);

        Assert.Equal(RoleInvitationCommandStatus.Success, result.Status);

        using var verifyDb = fixture.CreateContext();
        var withdrawnStatusId = verifyDb.RoleInvitationStatuses.Single(s => s.Code == RoleInvitationStatusCodes.Withdrawn).Id;
        var reloaded = verifyDb.RoleInvitations.Single(ri => ri.Id == invitation.Id);
        Assert.Equal(withdrawnStatusId, reloaded.RoleInvitationStatusId);
        Assert.NotNull(reloaded.RespondedAt);
        Assert.Empty(verifyDb.PendingRoleGrants.Where(g => g.SamAccountName == "withdrawuser1"));

        Assert.Contains(auditLog.Calls, c => c.Action == "roleInvitation.withdrawn" && c.EntityId == invitation.Id);
    }

    [Fact]
    public async Task WithdrawAsync_AppliedInvitation_ReturnsInvalidStatus_NoChange()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUser(fixture, "withdrawadmin2");
        var invitation = SeedRoleInvitation(fixture, "appliedone1", "evaluator", RoleInvitationStatusCodes.Applied, actorId);

        var lookup = new FakeAdIdentityLookupService(Array.Empty<AdIdentity>());
        var auditLog = new FakeAuditLogWriter();
        using var db = fixture.CreateContext();
        var service = CreateService(db, lookup, auditLog);

        var result = await service.WithdrawAsync(invitation.Id, actorId);

        Assert.Equal(RoleInvitationCommandStatus.InvalidStatus, result.Status);
        Assert.Empty(auditLog.Calls);

        using var verifyDb = fixture.CreateContext();
        var appliedStatusId = verifyDb.RoleInvitationStatuses.Single(s => s.Code == RoleInvitationStatusCodes.Applied).Id;
        var reloaded = verifyDb.RoleInvitations.Single(ri => ri.Id == invitation.Id);
        Assert.Equal(appliedStatusId, reloaded.RoleInvitationStatusId);
    }

    [Fact]
    public async Task WithdrawAsync_UnknownId_ReturnsNotFound()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUser(fixture, "withdrawadmin3");
        var lookup = new FakeAdIdentityLookupService(Array.Empty<AdIdentity>());
        using var db = fixture.CreateContext();
        var service = CreateService(db, lookup);

        var result = await service.WithdrawAsync(Guid.NewGuid(), actorId);

        Assert.Equal(RoleInvitationCommandStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task RemindAsync_PendingInvitationBelowCap_QueuesEmailAndIncrementsCount()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUser(fixture, "remindadmin1");
        var invitation = SeedRoleInvitation(fixture, "reminduser1", "evaluator", RoleInvitationStatusCodes.Pending, actorId, email: "reminduser1@gac-demo.sa", reminderCount: 1);

        var lookup = new FakeAdIdentityLookupService(Array.Empty<AdIdentity>());
        using var db = fixture.CreateContext();
        var service = CreateService(db, lookup);

        var result = await service.RemindAsync(invitation.Id);

        Assert.Equal(RoleInvitationCommandStatus.Success, result.Status);

        using var verifyDb = fixture.CreateContext();
        var reloaded = verifyDb.RoleInvitations.Single(ri => ri.Id == invitation.Id);
        Assert.Equal(2, reloaded.ReminderCount);
        Assert.NotNull(reloaded.LastReminderAt);

        var email = Assert.Single(verifyDb.EmailOutboxes.Where(e => e.Category == "role_invitation_reminder"));
        Assert.Equal("reminduser1@gac-demo.sa", email.ToEmail);
    }

    [Fact]
    public async Task RemindAsync_AtMaxReminders_ReturnsInvalidStatus_NoEmailQueued()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUser(fixture, "remindadmin2");
        var invitation = SeedRoleInvitation(fixture, "remindmaxuser1", "evaluator", RoleInvitationStatusCodes.Pending, actorId, email: "remindmaxuser1@gac-demo.sa", reminderCount: 3);

        var lookup = new FakeAdIdentityLookupService(Array.Empty<AdIdentity>());
        using var db = fixture.CreateContext();
        var service = CreateService(db, lookup);

        var result = await service.RemindAsync(invitation.Id);

        Assert.Equal(RoleInvitationCommandStatus.InvalidStatus, result.Status);

        using var verifyDb = fixture.CreateContext();
        var reloaded = verifyDb.RoleInvitations.Single(ri => ri.Id == invitation.Id);
        Assert.Equal(3, reloaded.ReminderCount);
        Assert.Empty(verifyDb.EmailOutboxes.Where(e => e.Category == "role_invitation_reminder"));
    }

    [Fact]
    public async Task BulkWithdrawAsync_AllSucceed_SameBehaviorAsSingleWithdraw()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUser(fixture, "bulkwithdrawadmin1");
        var invitation1 = SeedRoleInvitation(fixture, "bulkwithdraw1", "evaluator", RoleInvitationStatusCodes.Pending, actorId);
        SeedPendingRoleGrant(fixture, "bulkwithdraw1", "evaluator", actorId);
        var invitation2 = SeedRoleInvitation(fixture, "bulkwithdraw2", "judge", RoleInvitationStatusCodes.Pending, actorId);
        SeedPendingRoleGrant(fixture, "bulkwithdraw2", "judge", actorId);

        var lookup = new FakeAdIdentityLookupService(Array.Empty<AdIdentity>());
        var auditLog = new FakeAuditLogWriter();
        using var db = fixture.CreateContext();
        var service = CreateService(db, lookup, auditLog);

        var results = await service.BulkWithdrawAsync(new[] { invitation1.Id, invitation2.Id }, actorId);

        Assert.All(results, r => Assert.Equal(RoleInvitationCommandStatus.Success, r.Status));
        Assert.Equal(2, auditLog.Calls.Count);
        Assert.All(auditLog.Calls, c => Assert.Equal("roleInvitation.withdrawn", c.Action));

        using var verifyDb = fixture.CreateContext();
        var withdrawnStatusId = verifyDb.RoleInvitationStatuses.Single(s => s.Code == RoleInvitationStatusCodes.Withdrawn).Id;
        Assert.True(verifyDb.RoleInvitations.Where(ri => ri.Id == invitation1.Id || ri.Id == invitation2.Id).All(ri => ri.RoleInvitationStatusId == withdrawnStatusId));
        Assert.Empty(verifyDb.PendingRoleGrants.Where(g => g.SamAccountName == "bulkwithdraw1" || g.SamAccountName == "bulkwithdraw2"));
    }

    [Fact]
    public async Task GetRoleDetailAsync_UnknownRoleCode_ReturnsNull()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var service = CreateService(db, new FakeAdIdentityLookupService(Array.Empty<AdIdentity>()));

        var result = await service.GetRoleDetailAsync("not_a_real_role");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetRoleDetailAsync_KnownRoleWithMembersAndInvitations_ReturnsCorrectDetail()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUser(fixture, "detailadmin1");

        var evalUser1 = SeedUser(fixture, "detaileval1");
        var evalUser2 = SeedUser(fixture, "detaileval2");
        SeedUserRole(fixture, evalUser1, "evaluator");
        SeedUserRole(fixture, evalUser2, "evaluator");
        var pendingInvitation = SeedRoleInvitation(fixture, "detailpending1", "evaluator", RoleInvitationStatusCodes.Pending, actorId);
        var appliedInvitation = SeedRoleInvitation(fixture, "detailapplied1", "evaluator", RoleInvitationStatusCodes.Applied, actorId);

        using var db = fixture.CreateContext();
        var expectedRole = db.Roles.Single(r => r.Code == "evaluator");
        var service = CreateService(db, new FakeAdIdentityLookupService(Array.Empty<AdIdentity>()));

        var result = await service.GetRoleDetailAsync("evaluator");

        Assert.NotNull(result);
        Assert.Equal("evaluator", result!.RoleCode);
        Assert.Equal(expectedRole.NameAr, result.RoleNameAr);
        Assert.Equal(expectedRole.NameEn, result.RoleNameEn);

        Assert.Equal(2, result.ActiveMembers.Count);
        Assert.Contains(result.ActiveMembers, m => m.UserId == evalUser1 && m.SamAccountName == "detaileval1" && m.FullNameEn == "detaileval1");
        Assert.Contains(result.ActiveMembers, m => m.UserId == evalUser2 && m.SamAccountName == "detaileval2" && m.FullNameEn == "detaileval2");

        Assert.Equal(2, result.Invitations.Count);
        Assert.Contains(result.Invitations, i => i.Id == pendingInvitation.Id && i.SamAccountName == "detailpending1" && i.RoleInvitationStatus.Code == RoleInvitationStatusCodes.Pending);
        Assert.Contains(result.Invitations, i => i.Id == appliedInvitation.Id && i.SamAccountName == "detailapplied1" && i.RoleInvitationStatus.Code == RoleInvitationStatusCodes.Applied);
    }

    [Fact]
    public async Task GetRoleDetailAsync_RoleWithNoMembersOrInvitations_ReturnsEmptyListsNotNull()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var expectedRole = db.Roles.Single(r => r.Code == "admin");
        var service = CreateService(db, new FakeAdIdentityLookupService(Array.Empty<AdIdentity>()));

        var result = await service.GetRoleDetailAsync("admin");

        Assert.NotNull(result);
        Assert.Equal("admin", result!.RoleCode);
        Assert.Equal(expectedRole.NameAr, result.RoleNameAr);
        Assert.Equal(expectedRole.NameEn, result.RoleNameEn);
        Assert.Empty(result.ActiveMembers);
        Assert.Empty(result.Invitations);
    }

    [Fact]
    public async Task GetRoleDetailAsync_OtherRolesMembersAndInvitations_AreExcluded()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUser(fixture, "detailadmin2");

        var evalUser = SeedUser(fixture, "detailexcleval1");
        SeedUserRole(fixture, evalUser, "evaluator");
        var evalInvitation = SeedRoleInvitation(fixture, "detailexclpending1", "evaluator", RoleInvitationStatusCodes.Pending, actorId);

        var judgeUser = SeedUser(fixture, "detailexcljudge1");
        SeedUserRole(fixture, judgeUser, "judge");
        var judgeInvitation = SeedRoleInvitation(fixture, "detailexclpending2", "judge", RoleInvitationStatusCodes.Pending, actorId);

        using var db = fixture.CreateContext();
        var service = CreateService(db, new FakeAdIdentityLookupService(Array.Empty<AdIdentity>()));

        var result = await service.GetRoleDetailAsync("evaluator");

        Assert.NotNull(result);
        var member = Assert.Single(result!.ActiveMembers);
        Assert.Equal(evalUser, member.UserId);
        Assert.Equal("detailexcleval1", member.SamAccountName);
        Assert.DoesNotContain(result.ActiveMembers, m => m.UserId == judgeUser);

        var invitation = Assert.Single(result.Invitations);
        Assert.Equal(evalInvitation.Id, invitation.Id);
        Assert.DoesNotContain(result.Invitations, i => i.Id == judgeInvitation.Id);
    }

    [Fact]
    public async Task BulkRemindAsync_AllSucceed_SameBehaviorAsSingleRemind()
    {
        using var fixture = new SqliteContextFixture();
        var actorId = SeedUser(fixture, "bulkremindadmin1");
        var invitation1 = SeedRoleInvitation(fixture, "bulkremind1", "evaluator", RoleInvitationStatusCodes.Pending, actorId, email: "bulkremind1@gac-demo.sa");
        var invitation2 = SeedRoleInvitation(fixture, "bulkremind2", "judge", RoleInvitationStatusCodes.Pending, actorId, email: "bulkremind2@gac-demo.sa");

        var lookup = new FakeAdIdentityLookupService(Array.Empty<AdIdentity>());
        using var db = fixture.CreateContext();
        var service = CreateService(db, lookup);

        var results = await service.BulkRemindAsync(new[] { invitation1.Id, invitation2.Id });

        Assert.All(results, r => Assert.Equal(RoleInvitationCommandStatus.Success, r.Status));

        using var verifyDb = fixture.CreateContext();
        Assert.True(verifyDb.RoleInvitations.Where(ri => ri.Id == invitation1.Id || ri.Id == invitation2.Id).All(ri => ri.ReminderCount == 1 && ri.LastReminderAt != null));
        Assert.Equal(2, verifyDb.EmailOutboxes.Count(e => e.Category == "role_invitation_reminder"));
    }
}
