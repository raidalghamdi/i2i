using System.Net;
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

public class MeBadgesEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;

    public MeBadgesEndpointTests(WebApplicationFactory<Program> factory)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var lookup = new FakeAdIdentityLookupService(new[]
        {
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

                var submitterRoleId = db.Roles.Single(r => r.Code == "submitter").Id;
                var submitterId = Guid.NewGuid();
                db.Users.Add(new User { Id = submitterId, SamAccountName = "submitter1", Email = "submitter1@gac-demo.sa", FullNameAr = "s1", FullNameEn = "s1" });
                db.SaveChanges();
                db.Set<UserRole>().Add(new UserRole { UserId = submitterId, RoleId = submitterRoleId, IsPrimary = true });

                var earnedBadge = new Badge { Id = Guid.NewGuid(), Code = "first-idea", NameAr = "أول فكرة", NameEn = "First Idea" };
                var unearnedBadge = new Badge { Id = Guid.NewGuid(), Code = "prolific", NameAr = "غزير الإنتاج", NameEn = "Prolific" };
                db.Badges.AddRange(earnedBadge, unearnedBadge);
                db.SaveChanges();

                db.UserBadges.Add(new UserBadge { Id = Guid.NewGuid(), UserId = submitterId, BadgeId = earnedBadge.Id, AwardedAt = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc) });
                db.SaveChanges();
            });
        });
    }

    [Fact]
    public async Task Get_ReturnsFullCatalog_WithEarnedAtOnlyForOwnedBadges()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");

        var response = await client.GetAsync("/api/me/badges");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var badges = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("badges").EnumerateArray().ToList();
        Assert.Equal(2, badges.Count);

        var earned = badges.Single(b => b.GetProperty("code").GetString() == "first-idea");
        Assert.False(earned.GetProperty("earnedAt").ValueKind == JsonValueKind.Null);

        var unearned = badges.Single(b => b.GetProperty("code").GetString() == "prolific");
        Assert.True(unearned.GetProperty("earnedAt").ValueKind == JsonValueKind.Null);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
