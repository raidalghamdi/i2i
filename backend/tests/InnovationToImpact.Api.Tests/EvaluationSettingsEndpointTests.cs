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

namespace InnovationToImpact.Api.Tests;

public class EvaluationSettingsEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;

    public EvaluationSettingsEndpointTests(WebApplicationFactory<Program> factory)
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
                Seed(db);
            });
        });
    }

    private void Seed(InnovationDbContext db)
    {
        var adminRoleId = db.Roles.Single(r => r.Code == "admin").Id;
        var submitterRoleId = db.Roles.Single(r => r.Code == "submitter").Id;
        var adminId = Guid.NewGuid();
        db.Users.Add(new User { Id = adminId, SamAccountName = "admin1", Email = "admin1@gac-demo.sa", FullNameAr = "a1", FullNameEn = "a1" });
        var submitterId = Guid.NewGuid();
        db.Users.Add(new User { Id = submitterId, SamAccountName = "submitter1", Email = "submitter1@gac-demo.sa", FullNameAr = "s1", FullNameEn = "s1" });
        db.SaveChanges();
        db.Set<UserRole>().Add(new UserRole { UserId = adminId, RoleId = adminRoleId, IsPrimary = true });
        db.Set<UserRole>().Add(new UserRole { UserId = submitterId, RoleId = submitterRoleId, IsPrimary = true });
        db.SaveChanges();
    }

    [Fact]
    public async Task Get_AsAdmin_ReturnsSeededDefaultSix()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "admin1");
        var response = await client.GetAsync("/api/admin/evaluation/settings");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(6.0, body.GetProperty("passThreshold").GetDouble());
    }

    [Fact]
    public async Task Get_AsSubmitter_Forbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");
        var response = await client.GetAsync("/api/admin/evaluation/settings");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Patch_AsAdmin_ValidValue_UpdatesAndPersists()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "admin1");
        var patch = await client.PatchAsJsonAsync("/api/admin/evaluation/settings", new { passThreshold = 7.5 });
        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);
        var get = await client.GetAsync("/api/admin/evaluation/settings");
        var body = JsonDocument.Parse(await get.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(7.5, body.GetProperty("passThreshold").GetDouble());
        Assert.False(body.GetProperty("updatedAt").ValueKind == JsonValueKind.Null);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public async Task Patch_OutOfRange_BadRequest(double invalid)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "admin1");
        var patch = await client.PatchAsJsonAsync("/api/admin/evaluation/settings", new { passThreshold = invalid });
        Assert.Equal(HttpStatusCode.BadRequest, patch.StatusCode);
    }

    [Fact]
    public async Task Patch_MissingPassThreshold_BadRequest()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "admin1");
        var patch = await client.PatchAsJsonAsync("/api/admin/evaluation/settings", new { });
        Assert.Equal(HttpStatusCode.BadRequest, patch.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    public async Task Patch_BoundaryValue_Ok(double boundary)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "admin1");
        var patch = await client.PatchAsJsonAsync("/api/admin/evaluation/settings", new { passThreshold = boundary });
        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);
        var body = JsonDocument.Parse(await patch.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(boundary, body.GetProperty("passThreshold").GetDouble());
    }

    [Fact]
    public async Task Patch_AsSubmitter_Forbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "submitter1");
        var patch = await client.PatchAsJsonAsync("/api/admin/evaluation/settings", new { passThreshold = 5.0 });
        Assert.Equal(HttpStatusCode.Forbidden, patch.StatusCode);
    }

    public void Dispose() => _connection.Dispose();
}
