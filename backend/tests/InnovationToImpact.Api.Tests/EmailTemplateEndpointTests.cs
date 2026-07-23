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

public class EmailTemplateEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;

    public EmailTemplateEndpointTests(WebApplicationFactory<Program> factory)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var lookup = new FakeAdIdentityLookupService(new[]
        {
            new AdIdentity("supervisor1", "Supervisor One", "supervisor1@gac-demo.sa", null, null, null),
            new AdIdentity("evaluator1", "Evaluator One", "evaluator1@gac-demo.sa", null, null, null),
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

                var roleIds = db.Roles.ToDictionary(r => r.Code, r => r.Id);
                var supervisorId = Guid.NewGuid();
                db.Users.Add(new User { Id = supervisorId, SamAccountName = "supervisor1", Email = "supervisor1@gac-demo.sa", FullNameAr = "s1", FullNameEn = "s1" });
                var evaluatorId = Guid.NewGuid();
                db.Users.Add(new User { Id = evaluatorId, SamAccountName = "evaluator1", Email = "evaluator1@gac-demo.sa", FullNameAr = "e1", FullNameEn = "e1" });
                db.SaveChanges();
                db.Set<UserRole>().Add(new UserRole { UserId = supervisorId, RoleId = roleIds[RoleCodes.Supervisor], IsPrimary = true });
                db.Set<UserRole>().Add(new UserRole { UserId = evaluatorId, RoleId = roleIds[RoleCodes.Evaluator], IsPrimary = true });
                db.SaveChanges();
            });
        });
    }

    [Fact]
    public async Task List_AsSupervisor_ReturnsFourTemplates()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var response = await client.GetAsync("/api/admin/email-templates");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(4, body.GetArrayLength());
    }

    [Fact]
    public async Task List_AsEvaluator_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "evaluator1");

        var response = await client.GetAsync("/api/admin/email-templates");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Update_AsSupervisor_UpdatesSubjectAndBody()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");
        var listResponse = await client.GetAsync("/api/admin/email-templates");
        var list = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = list[0].GetProperty("id").GetGuid();

        var response = await client.PatchAsJsonAsync($"/api/admin/email-templates/{id}", new { subjectAr = "موضوع", subjectEn = "Subject", bodyAr = "نص", bodyEn = "Body", isBroadcast = true });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Subject", body.GetProperty("subjectEn").GetString());
        Assert.True(body.GetProperty("isBroadcast").GetBoolean());
    }

    [Fact]
    public async Task Update_NotFound_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Dev-User", "supervisor1");

        var response = await client.PatchAsJsonAsync($"/api/admin/email-templates/{Guid.NewGuid()}", new { subjectAr = "أ", subjectEn = "A", bodyAr = "ب", bodyEn = "B", isBroadcast = false });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    public void Dispose() => _connection.Dispose();
}
