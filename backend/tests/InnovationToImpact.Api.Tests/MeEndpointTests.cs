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
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace InnovationToImpact.Api.Tests;

public class MeEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;

    public MeEndpointTests(WebApplicationFactory<Program> factory)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var lookup = new FakeAdIdentityLookupService(new[]
        {
            new AdIdentity("submitter1", "Submitter One", "submitter1@gac-demo.sa", "IT", null, null),
        });

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration(config =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "DevAuth:SamAccountName", "" }
                });
            });
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
                db.Users.Add(new User
                {
                    Id = submitterId,
                    SamAccountName = "submitter1",
                    Email = "submitter1@gac-demo.sa",
                    FullNameAr = "المقدم واحد",
                    FullNameEn = "Submitter One",
                    Department = "IT",
                    Points = 40,
                    Level = 2,
                });
                db.SaveChanges();
                db.Set<UserRole>().Add(new UserRole { UserId = submitterId, RoleId = submitterRoleId, IsPrimary = true });
                db.SaveChanges();
            });
        });
    }

    [Fact]
    public async Task Get_ReturnsDbBackedProfile_NotJustClaims()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("Submitter One", body.GetProperty("fullNameEn").GetString());
        Assert.Equal("المقدم واحد", body.GetProperty("fullNameAr").GetString());
        Assert.Equal(40, body.GetProperty("points").GetInt32());
        Assert.Equal(2, body.GetProperty("level").GetInt32());
        Assert.Contains("submitter", body.GetProperty("roles").EnumerateArray().Select(r => r.GetString()));
    }

    [Fact]
    public async Task Get_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
