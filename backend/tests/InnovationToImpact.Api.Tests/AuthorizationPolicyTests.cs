using System.Net;
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

public class AuthorizationPolicyTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly WebApplicationFactory<Program> _unauthenticatedFactory;

    public AuthorizationPolicyTests(WebApplicationFactory<Program> factory)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var lookup = new FakeAdIdentityLookupService(new[]
        {
            new AdIdentity("admin1", "Admin One", "admin1@gac-demo.sa", null, null, null),
            new AdIdentity("supervisor1", "Supervisor One", "supervisor1@gac-demo.sa", null, null, null),
            new AdIdentity("judge1", "Judge One", "judge1@gac-demo.sa", null, null, null),
            new AdIdentity("evaluator1", "Evaluator One", "evaluator1@gac-demo.sa", null, null, null),
            new AdIdentity("submitter1", "Submitter One", "submitter1@gac-demo.sa", null, null, null),
            new AdIdentity("roleless1", "Roleless One", "roleless1@gac-demo.sa", null, null, null),
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
            });
        });

        // Separate factory, override DevAuth:SamAccountName to empty so DevAuthenticationHandler
        // has no fallback identity when no X-Dev-User header is sent either — this is the only way
        // to make a request through the Dev scheme that genuinely fails authentication (as opposed
        // to authenticating as some identity that then fails a role check, which is 403, not 401).
        _unauthenticatedFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["DevAuth:SamAccountName"] = "",
                }));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<InnovationDbContext>>();
                services.AddDbContext<InnovationDbContext>(options => options.UseSqlite(_connection));

                services.RemoveAll<IAdIdentityLookupService>();
                services.AddSingleton<IAdIdentityLookupService>(lookup);

                using var scope = services.BuildServiceProvider().CreateScope();
                scope.ServiceProvider.GetRequiredService<InnovationDbContext>().Database.EnsureCreated();
            });
        });
    }

    private static void SeedUsersAndRoles(InnovationDbContext db)
    {
        // Role rows for all 8 canonical RoleCodes are already present at this point: RoleConfiguration
        // (Task 1) seeds them via EF Core HasData with deterministic ids, and Database.EnsureCreated()
        // (called by the caller just before this method) applies that model-level seed data. Re-inserting
        // Role rows with the same Code here would violate the unique index on Role.Code, so look up the
        // existing ids instead of creating new ones.
        var roleIds = db.Roles.ToDictionary(r => r.Code, r => r.Id);

        AddUserWithRole(db, "admin1", RoleCodes.Admin, roleIds);
        AddUserWithRole(db, "supervisor1", RoleCodes.Supervisor, roleIds);
        AddUserWithRole(db, "judge1", RoleCodes.Judge, roleIds);
        AddUserWithRole(db, "evaluator1", RoleCodes.Evaluator, roleIds);
        AddUserWithRole(db, "submitter1", RoleCodes.Submitter, roleIds);

        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            SamAccountName = "roleless1",
            Email = "roleless1@gac-demo.sa",
            FullNameAr = "بدون دور",
            FullNameEn = "Roleless One",
        });
        db.SaveChanges();
    }

    private static void AddUserWithRole(InnovationDbContext db, string samAccountName, string roleCode, Dictionary<string, Guid> roleIds)
    {
        var userId = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = userId,
            SamAccountName = samAccountName,
            Email = $"{samAccountName}@gac-demo.sa",
            FullNameAr = samAccountName,
            FullNameEn = samAccountName,
        });
        db.SaveChanges();
        db.Set<UserRole>().Add(new UserRole { UserId = userId, RoleId = roleIds[roleCode], IsPrimary = true });
        db.SaveChanges();
    }

    [Theory]
    [InlineData("admin1", "/api/admin/ping", 200)]
    [InlineData("admin1", "/api/supervisor/ping", 200)]
    [InlineData("admin1", "/api/evaluator/ping", 200)]
    [InlineData("admin1", "/api/submitter/ping", 200)]
    [InlineData("supervisor1", "/api/admin/ping", 403)]
    [InlineData("supervisor1", "/api/supervisor/ping", 200)]
    [InlineData("supervisor1", "/api/evaluator/ping", 200)]
    [InlineData("supervisor1", "/api/submitter/ping", 200)]
    [InlineData("judge1", "/api/admin/ping", 403)]
    [InlineData("judge1", "/api/supervisor/ping", 200)]
    [InlineData("judge1", "/api/evaluator/ping", 200)]
    [InlineData("judge1", "/api/submitter/ping", 200)]
    [InlineData("evaluator1", "/api/admin/ping", 403)]
    [InlineData("evaluator1", "/api/supervisor/ping", 403)]
    [InlineData("evaluator1", "/api/evaluator/ping", 200)]
    [InlineData("evaluator1", "/api/submitter/ping", 200)]
    [InlineData("submitter1", "/api/admin/ping", 403)]
    [InlineData("submitter1", "/api/supervisor/ping", 403)]
    [InlineData("submitter1", "/api/evaluator/ping", 403)]
    [InlineData("submitter1", "/api/submitter/ping", 200)]
    [InlineData("roleless1", "/api/admin/ping", 403)]
    [InlineData("roleless1", "/api/supervisor/ping", 403)]
    [InlineData("roleless1", "/api/evaluator/ping", 403)]
    [InlineData("roleless1", "/api/submitter/ping", 403)]
    public async Task PolicyTierEnforcesExpectedAccess(string samAccountName, string path, int expectedStatus)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", samAccountName);

        var response = await client.GetAsync(path);

        Assert.Equal(expectedStatus, (int)response.StatusCode);
    }

    [Fact]
    public async Task UnauthenticatedRequest_Returns401NotForbidden()
    {
        var client = _unauthenticatedFactory.CreateClient();
        // No X-Dev-User header sent, and this factory's DevAuth:SamAccountName is overridden to
        // empty in the constructor — DevAuthenticationHandler.HandleAuthenticateAsync therefore
        // has no identity to fall back to and returns AuthenticateResult.Fail(...), producing a
        // genuinely unauthenticated request, distinct from the "authenticated but wrong role" 403
        // cases covered by the Theory above.

        var response = await client.GetAsync("/api/admin/ping");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    public void Dispose() => _connection.Dispose();
}
