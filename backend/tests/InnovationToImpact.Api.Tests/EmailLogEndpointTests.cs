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

public class EmailLogEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;

    public EmailLogEndpointTests(WebApplicationFactory<Program> factory)
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
                SeedEmailLogs(db);
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

    private static void SeedEmailLogs(InnovationDbContext db)
    {
        var sentStatusId = db.EmailLogStatuses.Single(s => s.Code == "sent").Id;
        var failedStatusId = db.EmailLogStatuses.Single(s => s.Code == "failed").Id;

        var now = DateTime.UtcNow;
        db.EmailLogs.Add(new EmailLog { Id = Guid.NewGuid(), Provider = "smtp", EmailLogStatusId = sentStatusId, ToEmail = "one@example.com", SentAt = now.AddMinutes(-30), ProviderMessageId = "msg-1", RedirectApplied = false });
        db.EmailLogs.Add(new EmailLog { Id = Guid.NewGuid(), Provider = "smtp", EmailLogStatusId = failedStatusId, ToEmail = "two@example.com", SentAt = now.AddMinutes(-20), ProviderMessageId = null, RedirectApplied = true });
        db.EmailLogs.Add(new EmailLog { Id = Guid.NewGuid(), Provider = "smtp", EmailLogStatusId = sentStatusId, ToEmail = "three@example.com", SentAt = now.AddMinutes(-10), ProviderMessageId = "msg-3", RedirectApplied = false });
        db.SaveChanges();
    }

    [Fact]
    public async Task AdminGetsItemsNewestFirst()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "admin1");

        var response = await client.GetAsync("/api/admin/email-log");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(3, body.GetProperty("total").GetInt32());
        Assert.Equal(3, body.GetProperty("items").GetArrayLength());
        Assert.Equal(1, body.GetProperty("page").GetInt32());
        Assert.Equal(25, body.GetProperty("pageSize").GetInt32());

        var items = body.GetProperty("items").EnumerateArray().ToList();
        Assert.Equal("three@example.com", items[0].GetProperty("toEmail").GetString());
        Assert.Equal("one@example.com", items[2].GetProperty("toEmail").GetString());

        var first = items[0];
        Assert.Equal("smtp", first.GetProperty("provider").GetString());
        Assert.Equal("sent", first.GetProperty("statusCode").GetString());
        Assert.Equal("Sent", first.GetProperty("statusNameEn").GetString());
        Assert.Equal("مرسل", first.GetProperty("statusNameAr").GetString());
        Assert.Equal("msg-3", first.GetProperty("providerMessageId").GetString());
        Assert.False(first.GetProperty("redirectApplied").GetBoolean());
        Assert.True(first.TryGetProperty("sentAt", out _));
        Assert.True(first.TryGetProperty("id", out _));
    }

    [Fact]
    public async Task FiltersByStatus()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "admin1");

        var response = await client.GetAsync("/api/admin/email-log?status=failed");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, body.GetProperty("total").GetInt32());
        var items = body.GetProperty("items").EnumerateArray().ToList();
        Assert.Single(items);
        Assert.Equal("failed", items[0].GetProperty("statusCode").GetString());
        Assert.Equal("two@example.com", items[0].GetProperty("toEmail").GetString());
    }

    [Fact]
    public async Task PagingCapsItemsButReturnsFullTotal()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "admin1");

        var response = await client.GetAsync("/api/admin/email-log?page=1&pageSize=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(3, body.GetProperty("total").GetInt32());
        Assert.Equal(2, body.GetProperty("items").GetArrayLength());
        Assert.Equal(1, body.GetProperty("page").GetInt32());
        Assert.Equal(2, body.GetProperty("pageSize").GetInt32());
    }

    [Fact]
    public async Task NonAdminGetsForbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");

        var response = await client.GetAsync("/api/admin/email-log");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    public void Dispose() => _connection.Dispose();
}
