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

public class NotificationsEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;
    private Guid _submitter1Id;
    private Guid _submitter2Id;
    private Guid _notif1Id;
    private Guid _notif2Id;

    public NotificationsEndpointTests(WebApplicationFactory<Program> factory)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var lookup = new FakeAdIdentityLookupService(new[]
        {
            new AdIdentity("submitter1", "Submitter One", "submitter1@gac-demo.sa", null, null, null),
            new AdIdentity("submitter2", "Submitter Two", "submitter2@gac-demo.sa", null, null, null),
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

                var submitterRoleId = db.Roles.Single(r => r.Code == "submitter").Id;
                _submitter1Id = Guid.NewGuid();
                _submitter2Id = Guid.NewGuid();
                db.Users.Add(new User { Id = _submitter1Id, SamAccountName = "submitter1", Email = "submitter1@gac-demo.sa", FullNameAr = "s1", FullNameEn = "s1" });
                db.Users.Add(new User { Id = _submitter2Id, SamAccountName = "submitter2", Email = "submitter2@gac-demo.sa", FullNameAr = "s2", FullNameEn = "s2" });
                db.SaveChanges();
                db.Set<UserRole>().Add(new UserRole { UserId = _submitter1Id, RoleId = submitterRoleId, IsPrimary = true });
                db.Set<UserRole>().Add(new UserRole { UserId = _submitter2Id, RoleId = submitterRoleId, IsPrimary = true });
                db.SaveChanges();

                _notif1Id = Guid.NewGuid();
                _notif2Id = Guid.NewGuid();
                db.Notifications.Add(new Notification { Id = _notif1Id, UserId = _submitter1Id, NotificationType = "idea_status", TitleAr = "ت1", TitleEn = "T1", BodyAr = "ب1", BodyEn = "B1", CreatedAt = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc) });
                db.Notifications.Add(new Notification { Id = _notif2Id, UserId = _submitter1Id, NotificationType = "idea_status", TitleAr = "ت2", TitleEn = "T2", BodyAr = "ب2", BodyEn = "B2", CreatedAt = new DateTime(2026, 7, 2, 0, 0, 0, DateTimeKind.Utc) });
                db.Notifications.Add(new Notification { Id = Guid.NewGuid(), UserId = _submitter2Id, NotificationType = "idea_status", TitleAr = "ت3", TitleEn = "T3", BodyAr = "ب3", BodyEn = "B3", CreatedAt = new DateTime(2026, 7, 3, 0, 0, 0, DateTimeKind.Utc) });
                db.SaveChanges();
            });
        });
    }

    [Fact]
    public async Task List_ReturnsOwnNotifications_NewestFirst()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");

        var response = await client.GetAsync("/api/notifications");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.EnumerateArray().ToList();
        Assert.Equal(2, items.Count);
        Assert.Equal("T2", items[0].GetProperty("titleEn").GetString());
        Assert.Equal("T1", items[1].GetProperty("titleEn").GetString());
    }

    [Fact]
    public async Task MarkRead_OwnNotification_SetsReadAt()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");

        var response = await client.PostAsync($"/api/notifications/{_notif1Id}/read", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.False(body.GetProperty("readAt").ValueKind == JsonValueKind.Null);
    }

    [Fact]
    public async Task MarkRead_AnotherUsersNotification_Returns404()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "submitter2");

        var response = await client.PostAsync($"/api/notifications/{_notif1Id}/read", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MarkAllRead_MarksOnlyOwnUnreadNotifications()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");

        var response = await client.PostAsync("/api/notifications/read-all", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(2, body.GetProperty("markedCount").GetInt32());

        var listResponse = await client.GetAsync("/api/notifications");
        var items = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync()).RootElement.EnumerateArray();
        Assert.All(items, item => Assert.False(item.GetProperty("readAt").ValueKind == JsonValueKind.Null));
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
