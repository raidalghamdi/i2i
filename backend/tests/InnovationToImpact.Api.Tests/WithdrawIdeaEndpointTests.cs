using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using InnovationToImpact.Domain.Auth;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Ideas;
using InnovationToImpact.Infrastructure.Auth;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InnovationToImpact.Api.Tests;

public class WithdrawIdeaEndpointTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;

    private Guid _submitterId;
    private Guid _otherSubmitterId;
    private Guid _evaluatorId;
    private Guid _submittedIdeaId;
    private Guid _approvedIdeaId;
    private Guid _withdrawnIdeaId;

    public WithdrawIdeaEndpointTests(WebApplicationFactory<Program> factory)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var lookup = new FakeAdIdentityLookupService(new[]
        {
            new AdIdentity("submitter1", "Submitter One", "submitter1@gac-demo.sa", "IT", null, null),
            new AdIdentity("submitter2", "Submitter Two", "submitter2@gac-demo.sa", "IT", null, null),
            new AdIdentity("evaluator1", "Evaluator One", "evaluator1@gac-demo.sa", "IT", null, null),
        });
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<InnovationDbContext>>();
                services.AddDbContext<InnovationDbContext>(o => o.UseSqlite(_connection));
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
        var submitterRole = db.Roles.Single(r => r.Code == RoleCodes.Submitter);
        var evaluatorRole = db.Roles.Single(r => r.Code == RoleCodes.Evaluator);

        var submitter = new User { Id = Guid.NewGuid(), SamAccountName = "submitter1", Email = "submitter1@gac-demo.sa", FullNameAr = "م", FullNameEn = "Submitter One", Department = "IT" };
        var otherSubmitter = new User { Id = Guid.NewGuid(), SamAccountName = "submitter2", Email = "submitter2@gac-demo.sa", FullNameAr = "م٢", FullNameEn = "Submitter Two", Department = "IT" };
        var evaluator = new User { Id = Guid.NewGuid(), SamAccountName = "evaluator1", Email = "evaluator1@gac-demo.sa", FullNameAr = "ق", FullNameEn = "Evaluator One", Department = "IT" };
        db.Users.AddRange(submitter, otherSubmitter, evaluator);
        db.SaveChanges();
        _submitterId = submitter.Id;
        _otherSubmitterId = otherSubmitter.Id;
        _evaluatorId = evaluator.Id;

        db.Set<UserRole>().AddRange(
            new UserRole { UserId = submitter.Id, RoleId = submitterRole.Id, IsPrimary = true },
            new UserRole { UserId = otherSubmitter.Id, RoleId = submitterRole.Id, IsPrimary = true },
            new UserRole { UserId = evaluator.Id, RoleId = evaluatorRole.Id, IsPrimary = true });
        db.SaveChanges();

        var theme = db.StrategicThemes.First();
        var submittedStatus = db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.Submitted);
        var approvedStatus = db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.Approved);
        var withdrawnStatus = db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.Withdrawn);

        Idea MkIdea(string code, IdeaStatus st) => new()
        {
            Id = Guid.NewGuid(), Code = code, TitleAr = "ع", TitleEn = code,
            ProblemStatementAr = "م", ProblemStatementEn = "P", ProposedSolutionAr = "ح", ProposedSolutionEn = "S",
            ExpectedBenefitsAr = "ب", ExpectedBenefitsEn = "B",
            StrategicThemeId = theme.Id, SubmitterId = submitter.Id, IdeaStatusId = st.Id, ParticipationType = "individual",
        };

        var submittedIdea = MkIdea("I-SUBMITTED", submittedStatus);
        var approvedIdea = MkIdea("I-APPROVED", approvedStatus);
        var withdrawnIdea = MkIdea("I-WITHDRAWN", withdrawnStatus);
        db.Ideas.AddRange(submittedIdea, approvedIdea, withdrawnIdea);
        db.SaveChanges();
        _submittedIdeaId = submittedIdea.Id;
        _approvedIdeaId = approvedIdea.Id;
        _withdrawnIdeaId = withdrawnIdea.Id;

        var pendingStatus = db.Set<AssignmentStatus>().Single(s => s.Code == "pending");
        db.Assignments.Add(new Assignment
        {
            Id = Guid.NewGuid(),
            IdeaId = submittedIdea.Id,
            EvaluatorId = evaluator.Id,
            AssignedById = submitter.Id,
            AssignedAt = DateTime.UtcNow,
            AssignmentStatusId = pendingStatus.Id,
        });
        db.SaveChanges();
    }

    private HttpClient ClientFor(string sam)
    {
        var c = _factory.CreateClient();
        c.DefaultRequestHeaders.Add("X-Dev-User", sam);
        return c;
    }

    [Fact]
    public async Task Withdraw_SubmittedIdeaByOwner_ReturnsOkWithdrawnStatus_AndWritesAuditLog()
    {
        var res = await ClientFor("submitter1").PostAsync($"/api/ideas/{_submittedIdeaId}/withdraw", null);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(_submittedIdeaId, body.GetProperty("id").GetGuid());
        Assert.Equal(IdeaStatusCodes.Withdrawn, body.GetProperty("status").GetString());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
        var idea = db.Ideas.Include(i => i.IdeaStatus).Single(i => i.Id == _submittedIdeaId);
        Assert.Equal(IdeaStatusCodes.Withdrawn, idea.IdeaStatus.Code);

        var auditEntry = db.AuditLogs.SingleOrDefault(a => a.EntityType == "idea" && a.EntityId == _submittedIdeaId && a.Action == "idea.withdrawn");
        Assert.NotNull(auditEntry);
        Assert.Equal(_submitterId, auditEntry!.ActorId);
    }

    [Fact]
    public async Task Withdraw_ByNonOwner_ReturnsForbidden()
    {
        var res = await ClientFor("submitter2").PostAsync($"/api/ideas/{_submittedIdeaId}/withdraw", null);

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Withdraw_ApprovedIdea_ReturnsConflict()
    {
        var res = await ClientFor("submitter1").PostAsync($"/api/ideas/{_approvedIdeaId}/withdraw", null);

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task Withdraw_AlreadyWithdrawnIdea_ReturnsConflict()
    {
        var res = await ClientFor("submitter1").PostAsync($"/api/ideas/{_withdrawnIdeaId}/withdraw", null);

        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task Withdraw_NotifiesAssignedEvaluator()
    {
        var res = await ClientFor("submitter1").PostAsync($"/api/ideas/{_submittedIdeaId}/withdraw", null);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
        var notification = db.Notifications.SingleOrDefault(n => n.UserId == _evaluatorId && n.NotificationType == "idea_withdrawn");
        Assert.NotNull(notification);
    }

    public void Dispose() => _connection.Dispose();
}
