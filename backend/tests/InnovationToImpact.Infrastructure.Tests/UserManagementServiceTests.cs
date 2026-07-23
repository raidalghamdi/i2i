using InnovationToImpact.Domain.Auth;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.UserManagement;
using InnovationToImpact.Infrastructure.Auth;
using InnovationToImpact.Infrastructure.UserManagement;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class UserManagementServiceTests
{
    private static Guid SeedUser(SqliteContextFixture fixture, string samAccountName)
    {
        using var db = fixture.CreateContext();
        var id = Guid.NewGuid();
        db.Users.Add(new User { Id = id, SamAccountName = samAccountName, Email = $"{samAccountName}@gac-demo.sa", FullNameAr = samAccountName, FullNameEn = samAccountName });
        db.SaveChanges();
        return id;
    }

    [Fact]
    public async Task GrantRoleAsync_ExistingUser_GrantsImmediately()
    {
        using var fixture = new SqliteContextFixture();
        var granterId = SeedUser(fixture, "granter1");
        var existingUserId = SeedUser(fixture, "existinguser1");
        using var db = fixture.CreateContext();
        var lookup = new FakeAdIdentityLookupService(new[] { new AdIdentity("existinguser1", "Existing User", "existinguser1@gac-demo.sa", null, null, null) });
        var service = new UserManagementService(db, lookup);

        var result = await service.GrantRoleAsync(new RoleGrantInput("existinguser1", "evaluator"), granterId);

        Assert.Equal(RoleGrantCommandStatus.GrantedImmediately, result.Status);
        Assert.Equal(existingUserId, result.User!.Id);

        using var verifyDb = fixture.CreateContext();
        var userRoles = await verifyDb.Set<UserRole>().Where(ur => ur.UserId == existingUserId).ToListAsync();
        Assert.Single(userRoles);
    }

    [Fact]
    public async Task GrantRoleAsync_UnknownUser_CreatesPendingGrant()
    {
        using var fixture = new SqliteContextFixture();
        var granterId = SeedUser(fixture, "granter2");
        using var db = fixture.CreateContext();
        var lookup = new FakeAdIdentityLookupService(new[] { new AdIdentity("futureuser1", "Future User", "futureuser1@gac-demo.sa", null, null, null) });
        var service = new UserManagementService(db, lookup);

        var result = await service.GrantRoleAsync(new RoleGrantInput("futureuser1", "evaluator"), granterId);

        Assert.Equal(RoleGrantCommandStatus.Pending, result.Status);
        Assert.NotNull(result.PendingGrant);

        using var verifyDb = fixture.CreateContext();
        Assert.Single(await verifyDb.PendingRoleGrants.Where(g => g.SamAccountName == "futureuser1").ToListAsync());
    }

    [Fact]
    public async Task GrantRoleAsync_AdUserNotFound_ReturnsAdUserNotFound()
    {
        using var fixture = new SqliteContextFixture();
        var granterId = SeedUser(fixture, "granter3");
        using var db = fixture.CreateContext();
        var lookup = new FakeAdIdentityLookupService(Array.Empty<AdIdentity>());
        var service = new UserManagementService(db, lookup);

        var result = await service.GrantRoleAsync(new RoleGrantInput("nobody", "evaluator"), granterId);

        Assert.Equal(RoleGrantCommandStatus.AdUserNotFound, result.Status);
    }

    [Fact]
    public async Task GrantRoleAsync_UnknownRoleCode_ReturnsRoleNotFound()
    {
        using var fixture = new SqliteContextFixture();
        var granterId = SeedUser(fixture, "granter4");
        using var db = fixture.CreateContext();
        var lookup = new FakeAdIdentityLookupService(new[] { new AdIdentity("someuser1", "Some User", "someuser1@gac-demo.sa", null, null, null) });
        var service = new UserManagementService(db, lookup);

        var result = await service.GrantRoleAsync(new RoleGrantInput("someuser1", "not_a_real_role"), granterId);

        Assert.Equal(RoleGrantCommandStatus.RoleNotFound, result.Status);
    }

    [Fact]
    public async Task GrantRoleAsync_AlreadyGranted_ReturnsAlreadyGranted()
    {
        using var fixture = new SqliteContextFixture();
        var granterId = SeedUser(fixture, "granter5");
        SeedUser(fixture, "doublegrant1");
        using var db = fixture.CreateContext();
        var lookup = new FakeAdIdentityLookupService(new[] { new AdIdentity("doublegrant1", "Double Grant", "doublegrant1@gac-demo.sa", null, null, null) });
        var service = new UserManagementService(db, lookup);
        await service.GrantRoleAsync(new RoleGrantInput("doublegrant1", "evaluator"), granterId);

        var result = await service.GrantRoleAsync(new RoleGrantInput("doublegrant1", "evaluator"), granterId);

        Assert.Equal(RoleGrantCommandStatus.AlreadyGranted, result.Status);
    }

    [Fact]
    public async Task GrantRoleAsync_AlreadyPending_ReturnsAlreadyPending()
    {
        using var fixture = new SqliteContextFixture();
        var granterId = SeedUser(fixture, "granter6");
        using var db = fixture.CreateContext();
        var lookup = new FakeAdIdentityLookupService(new[] { new AdIdentity("futureuser2", "Future User Two", "futureuser2@gac-demo.sa", null, null, null) });
        var service = new UserManagementService(db, lookup);
        await service.GrantRoleAsync(new RoleGrantInput("futureuser2", "evaluator"), granterId);

        var result = await service.GrantRoleAsync(new RoleGrantInput("futureuser2", "evaluator"), granterId);

        Assert.Equal(RoleGrantCommandStatus.AlreadyPending, result.Status);
    }

    [Fact]
    public async Task GrantRoleToGroupAsync_MixedExistingAndNewMembers_GrantsAndPendsCorrectly()
    {
        using var fixture = new SqliteContextFixture();
        var granterId = SeedUser(fixture, "granter7");
        SeedUser(fixture, "groupmember1");
        using var db = fixture.CreateContext();
        var lookup = new FakeAdIdentityLookupService(
            seedIdentities: new[]
            {
                new AdIdentity("groupmember1", "Group Member One", "groupmember1@gac-demo.sa", null, null, null),
                new AdIdentity("groupmember2", "Group Member Two", "groupmember2@gac-demo.sa", null, null, null),
            },
            groupMemberships: new Dictionary<string, IReadOnlyList<string>>
            {
                ["GAC-TestGroup"] = new[] { "groupmember1", "groupmember2" },
            });
        var service = new UserManagementService(db, lookup);

        var result = await service.GrantRoleToGroupAsync("GAC-TestGroup", "evaluator", granterId);

        Assert.Equal(1, result.GrantedCount);
        Assert.Equal(1, result.PendingCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task GrantRoleToGroupAsync_InvalidRoleCode_ReportsErrorsForEveryMember()
    {
        using var fixture = new SqliteContextFixture();
        var granterId = SeedUser(fixture, "granter8");
        using var db = fixture.CreateContext();
        var lookup = new FakeAdIdentityLookupService(
            seedIdentities: new[] { new AdIdentity("groupmember3", "Group Member Three", "groupmember3@gac-demo.sa", null, null, null) },
            groupMemberships: new Dictionary<string, IReadOnlyList<string>> { ["GAC-BadRoleGroup"] = new[] { "groupmember3" } });
        var service = new UserManagementService(db, lookup);

        var result = await service.GrantRoleToGroupAsync("GAC-BadRoleGroup", "not_a_real_role", granterId);

        Assert.Equal(0, result.GrantedCount);
        Assert.Equal(1, result.SkippedCount);
        Assert.Single(result.Errors);
    }

    [Fact]
    public async Task RevokeRoleAsync_ExistingAssignment_RemovesIt()
    {
        using var fixture = new SqliteContextFixture();
        var granterId = SeedUser(fixture, "granter9");
        var userId = SeedUser(fixture, "revokeuser1");
        using var db = fixture.CreateContext();
        var role = db.Roles.Single(r => r.Code == "evaluator");
        var lookup = new FakeAdIdentityLookupService(new[] { new AdIdentity("revokeuser1", "Revoke User", "revokeuser1@gac-demo.sa", null, null, null) });
        var service = new UserManagementService(db, lookup);
        await service.GrantRoleAsync(new RoleGrantInput("revokeuser1", "evaluator"), granterId);

        var result = await service.RevokeRoleAsync(userId, role.Id);

        Assert.Equal(UserManagementCommandStatus.Success, result.Status);
        using var verifyDb = fixture.CreateContext();
        Assert.Empty(await verifyDb.Set<UserRole>().Where(ur => ur.UserId == userId).ToListAsync());
    }

    [Fact]
    public async Task RevokeRoleAsync_NotFound_ReturnsNotFound()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var lookup = new FakeAdIdentityLookupService();
        var service = new UserManagementService(db, lookup);

        var result = await service.RevokeRoleAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(UserManagementCommandStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task SetActiveAsync_TogglesFlag()
    {
        using var fixture = new SqliteContextFixture();
        var userId = SeedUser(fixture, "toggleuser1");
        using var db = fixture.CreateContext();
        var lookup = new FakeAdIdentityLookupService();
        var service = new UserManagementService(db, lookup);

        var result = await service.SetActiveAsync(userId, false);

        Assert.Equal(UserManagementCommandStatus.Success, result.Status);
        using var verifyDb = fixture.CreateContext();
        Assert.False((await verifyDb.Users.SingleAsync(u => u.Id == userId)).IsActive);
    }

    [Fact]
    public async Task ListUsersAsync_ReturnsUsersWithRolesLoaded()
    {
        using var fixture = new SqliteContextFixture();
        var granterId = SeedUser(fixture, "granter10");
        SeedUser(fixture, "listeduser1");
        using var db = fixture.CreateContext();
        var lookup = new FakeAdIdentityLookupService(new[] { new AdIdentity("listeduser1", "Listed User", "listeduser1@gac-demo.sa", null, null, null) });
        var service = new UserManagementService(db, lookup);
        await service.GrantRoleAsync(new RoleGrantInput("listeduser1", "evaluator"), granterId);

        using var listDb = fixture.CreateContext();
        var listService = new UserManagementService(listDb, lookup);
        var users = await listService.ListUsersAsync();

        var listed = users.Single(u => u.SamAccountName == "listeduser1");
        Assert.Single(listed.UserRoles);
        Assert.Equal("evaluator", listed.UserRoles.First().Role.Code);
    }

    [Fact]
    public async Task GetUserDetailAsync_NotFound_ReturnsNull()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var lookup = new FakeAdIdentityLookupService();
        var service = new UserManagementService(db, lookup);

        var result = await service.GetUserDetailAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task ListPendingGrantsAsync_ReturnsGrantsWithRoleAndGranterLoaded()
    {
        using var fixture = new SqliteContextFixture();
        var granterId = SeedUser(fixture, "granter11");
        using var db = fixture.CreateContext();
        var lookup = new FakeAdIdentityLookupService(new[] { new AdIdentity("futureuser3", "Future User Three", "futureuser3@gac-demo.sa", null, null, null) });
        var service = new UserManagementService(db, lookup);
        await service.GrantRoleAsync(new RoleGrantInput("futureuser3", "evaluator"), granterId);

        using var listDb = fixture.CreateContext();
        var listService = new UserManagementService(listDb, lookup);
        var pending = await listService.ListPendingGrantsAsync();

        var grant = pending.Single(g => g.SamAccountName == "futureuser3");
        Assert.Equal("evaluator", grant.Role.Code);
        Assert.Equal("granter11", grant.GrantedBy.SamAccountName);
    }

    [Fact]
    public async Task CancelPendingGrantAsync_ExistingGrant_RemovesIt()
    {
        using var fixture = new SqliteContextFixture();
        var granterId = SeedUser(fixture, "granter12");
        using var db = fixture.CreateContext();
        var lookup = new FakeAdIdentityLookupService(new[] { new AdIdentity("futureuser4", "Future User Four", "futureuser4@gac-demo.sa", null, null, null) });
        var service = new UserManagementService(db, lookup);
        var granted = await service.GrantRoleAsync(new RoleGrantInput("futureuser4", "evaluator"), granterId);

        var result = await service.CancelPendingGrantAsync(granted.PendingGrant!.Id);

        Assert.Equal(UserManagementCommandStatus.Success, result.Status);
        using var verifyDb = fixture.CreateContext();
        Assert.Empty(await verifyDb.PendingRoleGrants.Where(g => g.SamAccountName == "futureuser4").ToListAsync());
    }

    [Fact]
    public async Task CancelPendingGrantAsync_NotFound_ReturnsNotFound()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var lookup = new FakeAdIdentityLookupService();
        var service = new UserManagementService(db, lookup);

        var result = await service.CancelPendingGrantAsync(Guid.NewGuid());

        Assert.Equal(UserManagementCommandStatus.NotFound, result.Status);
    }
}
