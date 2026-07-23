using System.Security.Claims;
using InnovationToImpact.Api.Auth;
using InnovationToImpact.Domain.Auth;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Roster;
using InnovationToImpact.Infrastructure.Auth;
using InnovationToImpact.Infrastructure.Tests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Xunit;

namespace InnovationToImpact.Api.Tests;

public class IdentityClaimsTransformationTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public IdentityClaimsTransformationTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    private static ActiveDirectoryOptions NewAdOptions() => new() { CacheTtlMinutes = 60 };

    private static ClaimsPrincipal PrincipalFor(string name)
    {
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, name) }, "Test");
        return new ClaimsPrincipal(identity);
    }

    [Fact]
    public async Task NewUser_SyncsIdentity_IssuesZeroRoleClaims()
    {
        using var db = _fixture.CreateContext();
        var lookup = new FakeAdIdentityLookupService(new[]
        {
            new AdIdentity("newuser1", "New User", "newuser1@gac-demo.sa", "Innovation", "Analyst", "mgr@gac-demo.sa")
        });
        var cache = new MemoryCache(new MemoryCacheOptions());
        var transformation = new IdentityClaimsTransformation(db, lookup, cache, Options.Create(NewAdOptions()));

        var result = await transformation.TransformAsync(PrincipalFor("newuser1"));

        Assert.Equal("newuser1@gac-demo.sa", result.FindFirstValue(ClaimTypes.Email));
        Assert.Empty(result.FindAll(ClaimTypes.Role));

        var user = await db.Users.SingleAsync(u => u.SamAccountName == "newuser1");
        Assert.Equal("New User", user.FullNameAr);
        Assert.Equal("New User", user.FullNameEn);
    }

    [Fact]
    public async Task ExistingUserWithRoles_IssuesRoleClaims_RefreshesAttributes_PreservesName()
    {
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        using (var seed = _fixture.CreateContext())
        {
            seed.Roles.Add(new Role { Id = roleId, Code = "test_custom_evaluator", NameAr = "مقيّم مخصص", NameEn = "Custom Evaluator", SortOrder = 100 });
            seed.Users.Add(new User
            {
                Id = userId,
                SamAccountName = "existinguser",
                Email = "stale@gac-demo.sa",
                FullNameAr = "اسم قديم",
                FullNameEn = "Old Name",
                Department = "OldDept"
            });
            seed.SaveChanges();
            seed.Set<UserRole>().Add(new UserRole { UserId = userId, RoleId = roleId, IsPrimary = true });
            seed.SaveChanges();
        }

        using var db = _fixture.CreateContext();
        var lookup = new FakeAdIdentityLookupService(new[]
        {
            new AdIdentity("existinguser", "New Display Name", "fresh@gac-demo.sa", "NewDept", "Senior Analyst", "mgr2@gac-demo.sa")
        });
        var cache = new MemoryCache(new MemoryCacheOptions());
        var transformation = new IdentityClaimsTransformation(db, lookup, cache, Options.Create(NewAdOptions()));

        var result = await transformation.TransformAsync(PrincipalFor("existinguser"));

        Assert.Contains("test_custom_evaluator", result.FindAll(ClaimTypes.Role).Select(c => c.Value));

        var user = await db.Users.SingleAsync(u => u.SamAccountName == "existinguser");
        Assert.Equal("fresh@gac-demo.sa", user.Email);
        Assert.Equal("NewDept", user.Department);
        Assert.Equal("Old Name", user.FullNameEn);
    }

    [Fact]
    public async Task CacheHit_DoesNotCallLookupServiceAgain()
    {
        using var db = _fixture.CreateContext();
        var lookup = new FakeAdIdentityLookupService(new[]
        {
            new AdIdentity("cacheduser", "Cached User", "cacheduser@gac-demo.sa", "Innovation", "Analyst", null)
        });
        var cache = new MemoryCache(new MemoryCacheOptions());
        var transformation = new IdentityClaimsTransformation(db, lookup, cache, Options.Create(NewAdOptions()));

        await transformation.TransformAsync(PrincipalFor("cacheduser"));
        await transformation.TransformAsync(PrincipalFor("cacheduser"));

        Assert.Equal(1, lookup.CallCount);
    }

    [Fact]
    public async Task LookupReturnsNull_ThrowsIdentityResolutionUnavailable()
    {
        using var db = _fixture.CreateContext();
        var lookup = new FakeAdIdentityLookupService(Array.Empty<AdIdentity>());
        var cache = new MemoryCache(new MemoryCacheOptions());
        var transformation = new IdentityClaimsTransformation(db, lookup, cache, Options.Create(NewAdOptions()));

        await Assert.ThrowsAsync<IdentityResolutionUnavailableException>(
            () => transformation.TransformAsync(PrincipalFor("ghost")));
    }

    [Fact]
    public async Task LookupThrows_WrapsInIdentityResolutionUnavailable()
    {
        using var db = _fixture.CreateContext();
        var lookup = new FakeAdIdentityLookupService(Array.Empty<AdIdentity>(), unavailableFor: new[] { "downuser" });
        var cache = new MemoryCache(new MemoryCacheOptions());
        var transformation = new IdentityClaimsTransformation(db, lookup, cache, Options.Create(NewAdOptions()));

        await Assert.ThrowsAsync<IdentityResolutionUnavailableException>(
            () => transformation.TransformAsync(PrincipalFor("downuser")));
    }

    [Fact]
    public async Task DomainPrefixedName_NormalizesToSamAccountNameOnly()
    {
        using var db = _fixture.CreateContext();
        var lookup = new FakeAdIdentityLookupService(new[]
        {
            new AdIdentity("prefixeduser", "Prefixed User", "prefixed@gac-demo.sa", null, null, null)
        });
        var cache = new MemoryCache(new MemoryCacheOptions());
        var transformation = new IdentityClaimsTransformation(db, lookup, cache, Options.Create(NewAdOptions()));

        await transformation.TransformAsync(PrincipalFor("CORP\\prefixeduser"));

        var user = await db.Users.SingleAsync(u => u.SamAccountName == "prefixeduser");
        Assert.Equal("prefixed@gac-demo.sa", user.Email);
    }

    [Fact]
    public async Task NewUser_WithPendingRoleGrant_AppliesGrantAndIssuesRoleClaim()
    {
        var roleId = Guid.NewGuid();
        var granterId = Guid.NewGuid();
        using (var seed = _fixture.CreateContext())
        {
            seed.Roles.Add(new Role { Id = roleId, Code = "test_pending_role", NameAr = "دور معلق", NameEn = "Pending Role", SortOrder = 101 });
            seed.Users.Add(new User { Id = granterId, SamAccountName = "granter1", Email = "granter1@gac-demo.sa", FullNameAr = "granter1", FullNameEn = "granter1" });
            seed.SaveChanges();
            seed.PendingRoleGrants.Add(new PendingRoleGrant { Id = Guid.NewGuid(), SamAccountName = "pendinguser1", RoleId = roleId, GrantedById = granterId });
            seed.SaveChanges();
        }

        using var db = _fixture.CreateContext();
        var lookup = new FakeAdIdentityLookupService(new[]
        {
            new AdIdentity("pendinguser1", "Pending User", "pendinguser1@gac-demo.sa", "Innovation", "Analyst", null)
        });
        var cache = new MemoryCache(new MemoryCacheOptions());
        var transformation = new IdentityClaimsTransformation(db, lookup, cache, Options.Create(NewAdOptions()));

        var result = await transformation.TransformAsync(PrincipalFor("pendinguser1"));

        Assert.Contains("test_pending_role", result.FindAll(ClaimTypes.Role).Select(c => c.Value));

        using var verifyDb = _fixture.CreateContext();
        Assert.Empty(await verifyDb.PendingRoleGrants.Where(g => g.SamAccountName == "pendinguser1").ToListAsync());
    }

    [Fact]
    public async Task NewUser_WithPendingRoleGrantAndMatchingRoleInvitation_MarksInvitationApplied()
    {
        var roleId = Guid.NewGuid();
        var granterId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();
        using (var seed = _fixture.CreateContext())
        {
            seed.Roles.Add(new Role { Id = roleId, Code = "test_invitation_apply_role", NameAr = "دور تطبيق الدعوة", NameEn = "Invitation Apply Role", SortOrder = 103 });
            seed.Users.Add(new User { Id = granterId, SamAccountName = "granter2", Email = "granter2@gac-demo.sa", FullNameAr = "granter2", FullNameEn = "granter2" });
            seed.SaveChanges();

            seed.PendingRoleGrants.Add(new PendingRoleGrant { Id = Guid.NewGuid(), SamAccountName = "pendinguser2", RoleId = roleId, GrantedById = granterId });

            var pendingStatusId = seed.RoleInvitationStatuses.Single(s => s.Code == RoleInvitationStatusCodes.Pending).Id;
            seed.RoleInvitations.Add(new RoleInvitation
            {
                Id = invitationId,
                SamAccountName = "pendinguser2",
                RoleId = roleId,
                Email = "pendinguser2@gac-demo.sa",
                RoleInvitationStatusId = pendingStatusId,
                DeadlineAt = DateTime.UtcNow.AddDays(14),
                Source = "manual",
                InvitedById = granterId,
            });
            seed.SaveChanges();
        }

        using var db = _fixture.CreateContext();
        var lookup = new FakeAdIdentityLookupService(new[]
        {
            new AdIdentity("pendinguser2", "Pending User Two", "pendinguser2@gac-demo.sa", "Innovation", "Analyst", null)
        });
        var cache = new MemoryCache(new MemoryCacheOptions());
        var transformation = new IdentityClaimsTransformation(db, lookup, cache, Options.Create(NewAdOptions()));

        var result = await transformation.TransformAsync(PrincipalFor("pendinguser2"));

        Assert.Contains("test_invitation_apply_role", result.FindAll(ClaimTypes.Role).Select(c => c.Value));

        using var verifyDb = _fixture.CreateContext();
        var user = await verifyDb.Users.SingleAsync(u => u.SamAccountName == "pendinguser2");
        Assert.True(await verifyDb.Set<UserRole>().AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == roleId));
        Assert.Empty(await verifyDb.PendingRoleGrants.Where(g => g.SamAccountName == "pendinguser2").ToListAsync());

        var invitation = await verifyDb.RoleInvitations.Include(ri => ri.RoleInvitationStatus).SingleAsync(ri => ri.Id == invitationId);
        Assert.Equal(RoleInvitationStatusCodes.Applied, invitation.RoleInvitationStatus.Code);
        Assert.NotNull(invitation.RespondedAt);
    }

    [Fact]
    public async Task InactiveUser_IssuesZeroRoleClaims_DespiteHavingUserRoles()
    {
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        using (var seed = _fixture.CreateContext())
        {
            seed.Roles.Add(new Role { Id = roleId, Code = "test_inactive_role", NameAr = "دور", NameEn = "Inactive Test Role", SortOrder = 102 });
            seed.Users.Add(new User { Id = userId, SamAccountName = "inactiveuser1", Email = "inactiveuser1@gac-demo.sa", FullNameAr = "inactiveuser1", FullNameEn = "inactiveuser1", IsActive = false });
            seed.SaveChanges();
            seed.Set<UserRole>().Add(new UserRole { UserId = userId, RoleId = roleId, IsPrimary = true });
            seed.SaveChanges();
        }

        using var db = _fixture.CreateContext();
        var lookup = new FakeAdIdentityLookupService(new[]
        {
            new AdIdentity("inactiveuser1", "Inactive User", "inactiveuser1@gac-demo.sa", "Innovation", "Analyst", null)
        });
        var cache = new MemoryCache(new MemoryCacheOptions());
        var transformation = new IdentityClaimsTransformation(db, lookup, cache, Options.Create(NewAdOptions()));

        var result = await transformation.TransformAsync(PrincipalFor("inactiveuser1"));

        Assert.Empty(result.FindAll(ClaimTypes.Role));
    }
}
