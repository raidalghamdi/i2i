using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using InnovationToImpact.Domain.Auth;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Roster;
using InnovationToImpact.Infrastructure.Auth;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace InnovationToImpact.Api.Tests;

public class RosterEndpointsTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;
    protected Guid SupervisorId;
    protected Guid EvaluatorId;
    protected Guid InvitationId;

    public RosterEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var lookup = new FakeAdIdentityLookupService(new[]
        {
            new AdIdentity("supervisor1", "Supervisor One", "supervisor1@gac-demo.sa", null, null, null),
            new AdIdentity("evaluator1", "Evaluator One", "evaluator1@gac-demo.sa", null, null, null),
            new AdIdentity("submitter1", "Submitter One", "submitter1@gac-demo.sa", null, null, null),
            new AdIdentity("newuser1", "New User One", "newuser1@gac-demo.sa", null, null, null),
            new AdIdentity("newuser2", "New User Two", "newuser2@gac-demo.sa", null, null, null),
            new AdIdentity("newuser3", "New User Three", "newuser3@gac-demo.sa", null, null, null),
            new AdIdentity("newuser4", "New User Four", "newuser4@gac-demo.sa", null, null, null),
            new AdIdentity("newuser5", "New User Five", "newuser5@gac-demo.sa", null, null, null),
        });

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<InnovationDbContext>>();
                services.AddDbContext<InnovationDbContext>(options => options.UseSqlite(_connection));

                services.RemoveAll<IAdIdentityLookupService>();
                services.AddSingleton<IAdIdentityLookupService>(lookup);

                using var scope = services.BuildServiceProvider().CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
                db.Database.EnsureCreated();
                Seed(db);
            });
        });
    }

    private void Seed(InnovationDbContext db)
    {
        var supervisorRoleId = db.Roles.Single(r => r.Code == RoleCodes.Supervisor).Id;
        var evaluatorRoleId = db.Roles.Single(r => r.Code == RoleCodes.Evaluator).Id;
        var submitterRoleId = db.Roles.Single(r => r.Code == RoleCodes.Submitter).Id;

        SupervisorId = Guid.NewGuid();
        db.Users.Add(new User { Id = SupervisorId, SamAccountName = "supervisor1", Email = "supervisor1@gac-demo.sa", FullNameAr = "s1", FullNameEn = "Supervisor One", IsActive = true });
        EvaluatorId = Guid.NewGuid();
        db.Users.Add(new User { Id = EvaluatorId, SamAccountName = "evaluator1", Email = "evaluator1@gac-demo.sa", FullNameAr = "e1", FullNameEn = "Evaluator One", IsActive = true });
        var submitterId = Guid.NewGuid();
        db.Users.Add(new User { Id = submitterId, SamAccountName = "submitter1", Email = "submitter1@gac-demo.sa", FullNameAr = "sub1", FullNameEn = "Submitter One", IsActive = true });
        db.SaveChanges();

        db.Set<UserRole>().Add(new UserRole { UserId = SupervisorId, RoleId = supervisorRoleId, IsPrimary = true });
        db.Set<UserRole>().Add(new UserRole { UserId = EvaluatorId, RoleId = evaluatorRoleId, IsPrimary = true });
        db.Set<UserRole>().Add(new UserRole { UserId = submitterId, RoleId = submitterRoleId, IsPrimary = true });
        db.SaveChanges();

        var pendingStatusId = db.RoleInvitationStatuses.Single(s => s.Code == RoleInvitationStatusCodes.Pending).Id;
        InvitationId = Guid.NewGuid();
        db.RoleInvitations.Add(new RoleInvitation
        {
            Id = InvitationId,
            SamAccountName = "submitter1",
            RoleId = evaluatorRoleId,
            DisplayName = "Submitter One",
            Email = "submitter1@gac-demo.sa",
            RoleInvitationStatusId = pendingStatusId,
            DeadlineAt = DateTime.UtcNow.AddDays(14),
            ReminderCount = 0,
            Source = "manual",
            InvitedById = SupervisorId,
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task Hub_AsSupervisor_ReturnsOneRowPerSeededRole()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var response = await client.GetAsync("/api/admin/roster");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
        var roleCount = db.Roles.Count();
        Assert.Equal(roleCount, body.GetArrayLength());

        var evaluatorRow = body.EnumerateArray().Single(r => r.GetProperty("roleCode").GetString() == RoleCodes.Evaluator);
        Assert.True(evaluatorRow.TryGetProperty("roleNameAr", out _));
        Assert.True(evaluatorRow.TryGetProperty("roleNameEn", out _));
        Assert.Equal(1, evaluatorRow.GetProperty("activeCount").GetInt32());
        Assert.Equal(1, evaluatorRow.GetProperty("pendingCount").GetInt32());
        Assert.Equal(0, evaluatorRow.GetProperty("expiredCount").GetInt32());
        Assert.Equal(0, evaluatorRow.GetProperty("withdrawnCount").GetInt32());
    }

    [Fact]
    public async Task Hub_AsEvaluator_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "evaluator1");

        var response = await client.GetAsync("/api/admin/roster");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Detail_AsSupervisor_UnknownRoleCode_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var response = await client.GetAsync("/api/admin/roster/not-a-real-role");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Detail_AsSupervisor_KnownRole_ReturnsActiveMembersAndInvitations()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var response = await client.GetAsync($"/api/admin/roster/{RoleCodes.Evaluator}");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(RoleCodes.Evaluator, body.GetProperty("roleCode").GetString());

        var activeMembers = body.GetProperty("activeMembers");
        Assert.Equal(1, activeMembers.GetArrayLength());
        Assert.Equal("evaluator1", activeMembers[0].GetProperty("samAccountName").GetString());
        Assert.Equal(EvaluatorId, activeMembers[0].GetProperty("userId").GetGuid());

        var invitations = body.GetProperty("invitations");
        Assert.Equal(1, invitations.GetArrayLength());
        Assert.Equal(InvitationId, invitations[0].GetProperty("id").GetGuid());
        Assert.Equal("pending", invitations[0].GetProperty("status").GetString());
        Assert.Equal("Supervisor One", invitations[0].GetProperty("invitedByName").GetString());
    }

    [Fact]
    public async Task Detail_AsEvaluator_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "evaluator1");

        var response = await client.GetAsync($"/api/admin/roster/{RoleCodes.Evaluator}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Settings_AsSupervisor_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var response = await client.GetAsync("/api/admin/roster/settings");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Settings_AsAdmin_ReturnsSeededDefaults()
    {
        var adminLookup = new FakeAdIdentityLookupService(new[]
        {
            new AdIdentity("admin1", "Admin One", "admin1@gac-demo.sa", null, null, null),
        });

        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IAdIdentityLookupService>();
                services.AddSingleton<IAdIdentityLookupService>(adminLookup);

                using var scope = services.BuildServiceProvider().CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
                var adminRoleId = db.Roles.Single(r => r.Code == RoleCodes.Admin).Id;
                var adminId = Guid.NewGuid();
                db.Users.Add(new User { Id = adminId, SamAccountName = "admin1", Email = "admin1@gac-demo.sa", FullNameAr = "a1", FullNameEn = "a1" });
                db.SaveChanges();
                db.Set<UserRole>().Add(new UserRole { UserId = adminId, RoleId = adminRoleId, IsPrimary = true });
                db.SaveChanges();
            });
        });

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "admin1");

        var response = await client.GetAsync("/api/admin/roster/settings");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(body.GetProperty("enabled").GetBoolean());
        Assert.Equal(14, body.GetProperty("defaultExpiresDays").GetInt32());
        Assert.Equal(48, body.GetProperty("reminderGapHours").GetInt32());
        Assert.Equal(3, body.GetProperty("maxReminders").GetInt32());
        Assert.True(body.TryGetProperty("updatedAt", out _));
    }

    [Fact]
    public async Task Invite_Single_Success_ReturnsCreatedOne()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var response = await client.PostAsJsonAsync($"/api/admin/roster/{RoleCodes.Evaluator}/invite", new
        {
            samAccountNames = new[] { "newuser1" },
            deadlineAt = (DateTime?)null,
        });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, body.GetProperty("total").GetInt32());
        Assert.Equal(1, body.GetProperty("created").GetInt32());
        Assert.Equal(0, body.GetProperty("skipped").GetInt32());
        Assert.Equal(0, body.GetProperty("errors").GetArrayLength());
    }

    [Fact]
    public async Task Invite_Single_AlreadyApplied_ReturnsSkippedWithError()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var response = await client.PostAsJsonAsync($"/api/admin/roster/{RoleCodes.Evaluator}/invite", new
        {
            samAccountNames = new[] { "evaluator1" },
        });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, body.GetProperty("total").GetInt32());
        Assert.Equal(0, body.GetProperty("created").GetInt32());
        Assert.Equal(1, body.GetProperty("skipped").GetInt32());
        var error = body.GetProperty("errors")[0];
        Assert.Equal("evaluator1", error.GetProperty("samAccountName").GetString());
        Assert.Equal("AlreadyApplied", error.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Invite_Single_AlreadyPending_ReturnsSkippedWithError()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        // submitter1 already has a seeded Pending invitation for the evaluator role.
        var response = await client.PostAsJsonAsync($"/api/admin/roster/{RoleCodes.Evaluator}/invite", new
        {
            samAccountNames = new[] { "submitter1" },
        });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, body.GetProperty("created").GetInt32());
        Assert.Equal(1, body.GetProperty("skipped").GetInt32());
        var error = body.GetProperty("errors")[0];
        Assert.Equal("submitter1", error.GetProperty("samAccountName").GetString());
        Assert.Equal("AlreadyPending", error.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Invite_Single_AdUserNotFound_ReturnsSkippedWithError()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var response = await client.PostAsJsonAsync($"/api/admin/roster/{RoleCodes.Evaluator}/invite", new
        {
            samAccountNames = new[] { "ghostuser" },
        });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, body.GetProperty("created").GetInt32());
        Assert.Equal(1, body.GetProperty("skipped").GetInt32());
        var error = body.GetProperty("errors")[0];
        Assert.Equal("ghostuser", error.GetProperty("samAccountName").GetString());
        Assert.Equal("AdUserNotFound", error.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Invite_Single_RoleNotFound_ReturnsSkippedWithError()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var response = await client.PostAsJsonAsync("/api/admin/roster/not-a-real-role/invite", new
        {
            samAccountNames = new[] { "newuser2" },
        });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, body.GetProperty("created").GetInt32());
        Assert.Equal(1, body.GetProperty("skipped").GetInt32());
        var error = body.GetProperty("errors")[0];
        Assert.Equal("newuser2", error.GetProperty("samAccountName").GetString());
        Assert.Equal("RoleNotFound", error.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Invite_Bulk_MixedResults_ReturnsErrorsInRequestOrder()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var response = await client.PostAsJsonAsync($"/api/admin/roster/{RoleCodes.Evaluator}/invite", new
        {
            samAccountNames = new[] { "ghostuser", "evaluator1", "newuser3" },
        });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, body.GetProperty("total").GetInt32());
        Assert.Equal(1, body.GetProperty("created").GetInt32());
        Assert.Equal(2, body.GetProperty("skipped").GetInt32());

        var errors = body.GetProperty("errors").EnumerateArray().ToList();
        Assert.Equal(2, errors.Count);
        Assert.Equal("ghostuser", errors[0].GetProperty("samAccountName").GetString());
        Assert.Equal("AdUserNotFound", errors[0].GetProperty("message").GetString());
        Assert.Equal("evaluator1", errors[1].GetProperty("samAccountName").GetString());
        Assert.Equal("AlreadyApplied", errors[1].GetProperty("message").GetString());
    }

    [Fact]
    public async Task Withdraw_Success_MarksWithdrawn()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var response = await client.PostAsync($"/api/admin/roster/{InvitationId}/withdraw", null);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(InvitationId, body.GetProperty("id").GetGuid());
        Assert.Equal("withdrawn", body.GetProperty("status").GetString());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
        var invitation = db.RoleInvitations.Include(i => i.RoleInvitationStatus).Single(i => i.Id == InvitationId);
        Assert.Equal(RoleInvitationStatusCodes.Withdrawn, invitation.RoleInvitationStatus.Code);
    }

    [Fact]
    public async Task Withdraw_AlreadyWithdrawn_ReturnsConflict()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var first = await client.PostAsync($"/api/admin/roster/{InvitationId}/withdraw", null);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await client.PostAsync($"/api/admin/roster/{InvitationId}/withdraw", null);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Withdraw_UnknownId_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var response = await client.PostAsync($"/api/admin/roster/{Guid.NewGuid()}/withdraw", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task WithdrawBulk_MixedIds_ReturnsWithdrawnCount()
    {
        Guid secondInvitationId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
            var evaluatorRoleId = db.Roles.Single(r => r.Code == RoleCodes.Evaluator).Id;
            var pendingStatusId = db.RoleInvitationStatuses.Single(s => s.Code == RoleInvitationStatusCodes.Pending).Id;
            secondInvitationId = Guid.NewGuid();
            db.RoleInvitations.Add(new RoleInvitation
            {
                Id = secondInvitationId,
                SamAccountName = "newuser4",
                RoleId = evaluatorRoleId,
                DisplayName = "New User Four",
                Email = "newuser4@gac-demo.sa",
                RoleInvitationStatusId = pendingStatusId,
                DeadlineAt = DateTime.UtcNow.AddDays(14),
                ReminderCount = 0,
                Source = "manual",
                InvitedById = SupervisorId,
            });
            db.SaveChanges();
        }

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var response = await client.PostAsJsonAsync("/api/admin/roster/withdraw-bulk", new
        {
            ids = new[] { InvitationId, secondInvitationId, Guid.NewGuid() },
        });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, body.GetProperty("withdrawn").GetInt32());
    }

    [Fact]
    public async Task Remind_Success_IncrementsReminderCount()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var response = await client.PostAsync($"/api/admin/roster/{InvitationId}/remind", null);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(InvitationId, body.GetProperty("id").GetGuid());
        Assert.Equal(1, body.GetProperty("reminderCount").GetInt32());
    }

    [Fact]
    public async Task Remind_AtMaxReminders_ReturnsConflict()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        // Seeded settings default maxReminders = 3.
        for (var i = 0; i < 3; i++)
        {
            var okResponse = await client.PostAsync($"/api/admin/roster/{InvitationId}/remind", null);
            Assert.Equal(HttpStatusCode.OK, okResponse.StatusCode);
        }

        var overCapResponse = await client.PostAsync($"/api/admin/roster/{InvitationId}/remind", null);
        Assert.Equal(HttpStatusCode.Conflict, overCapResponse.StatusCode);
    }

    [Fact]
    public async Task RemindBulk_ReturnsRemindedCount()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var response = await client.PostAsJsonAsync("/api/admin/roster/remind-bulk", new
        {
            ids = new[] { InvitationId, Guid.NewGuid() },
        });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, body.GetProperty("reminded").GetInt32());
    }

    [Fact]
    public async Task SettingsPatch_AsAdmin_PersistsPartialUpdate()
    {
        var adminLookup = new FakeAdIdentityLookupService(new[]
        {
            new AdIdentity("admin1", "Admin One", "admin1@gac-demo.sa", null, null, null),
        });

        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IAdIdentityLookupService>();
                services.AddSingleton<IAdIdentityLookupService>(adminLookup);

                using var scope = services.BuildServiceProvider().CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
                var adminRoleId = db.Roles.Single(r => r.Code == RoleCodes.Admin).Id;
                var adminId = Guid.NewGuid();
                db.Users.Add(new User { Id = adminId, SamAccountName = "admin1", Email = "admin1@gac-demo.sa", FullNameAr = "a1", FullNameEn = "a1" });
                db.SaveChanges();
                db.Set<UserRole>().Add(new UserRole { UserId = adminId, RoleId = adminRoleId, IsPrimary = true });
                db.SaveChanges();
            });
        });

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "admin1");

        var response = await client.PatchAsJsonAsync("/api/admin/roster/settings", new
        {
            maxReminders = 5,
        });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(5, body.GetProperty("maxReminders").GetInt32());
        // Untouched fields keep their seeded defaults.
        Assert.True(body.GetProperty("enabled").GetBoolean());
        Assert.Equal(14, body.GetProperty("defaultExpiresDays").GetInt32());
        Assert.Equal(48, body.GetProperty("reminderGapHours").GetInt32());
    }

    [Fact]
    public async Task EmployeeImport_MixedRows_ReturnsSameShapeAsRosterInvite()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var response = await client.PostAsJsonAsync("/api/admin/employees/import", new
        {
            rows = new[]
            {
                new { samAccountName = "newuser5", roleCode = RoleCodes.Evaluator },
                new { samAccountName = "evaluator1", roleCode = RoleCodes.Evaluator },
                new { samAccountName = "ghostuser2", roleCode = RoleCodes.Evaluator },
            },
        });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, body.GetProperty("total").GetInt32());
        Assert.Equal(1, body.GetProperty("created").GetInt32());
        Assert.Equal(2, body.GetProperty("skipped").GetInt32());

        var errors = body.GetProperty("errors").EnumerateArray().ToList();
        Assert.Equal(2, errors.Count);
        Assert.Equal("evaluator1", errors[0].GetProperty("samAccountName").GetString());
        Assert.Equal("AlreadyApplied", errors[0].GetProperty("message").GetString());
        Assert.Equal("ghostuser2", errors[1].GetProperty("samAccountName").GetString());
        Assert.Equal("AdUserNotFound", errors[1].GetProperty("message").GetString());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
        var imported = db.RoleInvitations.Single(i => i.SamAccountName == "newuser5");
        Assert.Equal("import", imported.Source);
    }

    public void Dispose() => _connection.Dispose();
}
