using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using InnovationToImpact.Domain.Auth;
using InnovationToImpact.Domain.Entities;
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

public class SupportInboxEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;
    private Guid _handledId;
    private Guid _unhandledId1;
    private Guid _unhandledId2;

    public SupportInboxEndpointTests(WebApplicationFactory<Program> factory)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var lookup = new FakeAdIdentityLookupService(new[]
        {
            new AdIdentity("admin1", "Admin One", "admin1@gac-demo.sa", null, null, null),
            new AdIdentity("submitter1", "Submitter One", "submitter1@gac-demo.sa", null, null, null),
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
                SeedUsersAndRoles(db);
                SeedSupportMessages(db);
            });
        });
    }

    private static void SeedUsersAndRoles(InnovationDbContext db)
    {
        var roleIds = db.Roles.ToDictionary(r => r.Code, r => r.Id);

        var adminId = Guid.NewGuid();
        db.Users.Add(new User { Id = adminId, SamAccountName = "admin1", Email = "admin1@gac-demo.sa", FullNameAr = "admin1", FullNameEn = "admin1" });
        var submitterId = Guid.NewGuid();
        db.Users.Add(new User { Id = submitterId, SamAccountName = "submitter1", Email = "submitter1@gac-demo.sa", FullNameAr = "submitter1", FullNameEn = "submitter1" });
        db.SaveChanges();

        db.Set<UserRole>().Add(new UserRole { UserId = adminId, RoleId = roleIds[RoleCodes.Admin], IsPrimary = true });
        db.Set<UserRole>().Add(new UserRole { UserId = submitterId, RoleId = roleIds[RoleCodes.Submitter], IsPrimary = true });
        db.SaveChanges();
    }

    private void SeedSupportMessages(InnovationDbContext db)
    {
        var now = DateTime.UtcNow;

        _unhandledId1 = Guid.NewGuid();
        db.SupportMessages.Add(new SupportMessage
        {
            Id = _unhandledId1,
            Name = "Jane Doe",
            Email = "jane@example.com",
            Subject = "Question one",
            Body = "First message body.",
            Handled = false,
            CreatedAt = now.AddMinutes(-30),
        });

        _handledId = Guid.NewGuid();
        db.SupportMessages.Add(new SupportMessage
        {
            Id = _handledId,
            Name = "John Roe",
            Email = "john@example.com",
            Subject = "Question two",
            Body = "Second message body.",
            Handled = true,
            CreatedAt = now.AddMinutes(-20),
        });

        _unhandledId2 = Guid.NewGuid();
        db.SupportMessages.Add(new SupportMessage
        {
            Id = _unhandledId2,
            Name = "Amy Poe",
            Email = "amy@example.com",
            Subject = "Question three",
            Body = "Third message body.",
            Handled = false,
            CreatedAt = now.AddMinutes(-10),
        });

        db.SaveChanges();
    }

    [Fact]
    public async Task AdminGetsItemsNewestFirst()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "admin1");

        var response = await client.GetAsync("/api/admin/support");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(3, body.GetProperty("total").GetInt32());
        Assert.Equal(3, body.GetProperty("items").GetArrayLength());
        Assert.Equal(1, body.GetProperty("page").GetInt32());
        Assert.Equal(25, body.GetProperty("pageSize").GetInt32());

        var items = body.GetProperty("items").EnumerateArray().ToList();
        Assert.Equal("amy@example.com", items[0].GetProperty("email").GetString());
        Assert.Equal("jane@example.com", items[2].GetProperty("email").GetString());

        var first = items[0];
        Assert.Equal("Amy Poe", first.GetProperty("name").GetString());
        Assert.Equal("Question three", first.GetProperty("subject").GetString());
        Assert.Equal("Third message body.", first.GetProperty("body").GetString());
        Assert.False(first.GetProperty("handled").GetBoolean());
        Assert.True(first.TryGetProperty("createdAt", out _));
        Assert.True(first.TryGetProperty("id", out _));
    }

    [Fact]
    public async Task FiltersByHandled()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "admin1");

        var response = await client.GetAsync("/api/admin/support?handled=true");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, body.GetProperty("total").GetInt32());
        var items = body.GetProperty("items").EnumerateArray().ToList();
        Assert.Single(items);
        Assert.True(items[0].GetProperty("handled").GetBoolean());
        Assert.Equal("john@example.com", items[0].GetProperty("email").GetString());
    }

    [Fact]
    public async Task MarkHandled_FlipsFlagAndWritesAudit()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "admin1");

        var response = await client.PostAsync($"/api/admin/support/{_unhandledId1}/handled", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(_unhandledId1, body.GetProperty("id").GetGuid());
        Assert.True(body.GetProperty("handled").GetBoolean());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
        var saved = await db.SupportMessages.SingleAsync(m => m.Id == _unhandledId1);
        Assert.True(saved.Handled);

        var auditRow = await db.AuditLogs.SingleAsync(a => a.EntityId == _unhandledId1 && a.Action == "support_message.handled");
        Assert.Equal("support_message", auditRow.EntityType);
    }

    [Fact]
    public async Task MarkHandled_MissingId_Returns404()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "admin1");

        var response = await client.PostAsync($"/api/admin/support/{Guid.NewGuid()}/handled", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task NonAdminGetsForbidden_OnList()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");

        var response = await client.GetAsync("/api/admin/support");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task NonAdminGetsForbidden_OnMarkHandled()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");

        var response = await client.PostAsync($"/api/admin/support/{_unhandledId1}/handled", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    public void Dispose() => _connection.Dispose();
}
