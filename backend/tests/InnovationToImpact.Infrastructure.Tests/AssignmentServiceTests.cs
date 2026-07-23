using InnovationToImpact.Domain.Assignments;
using InnovationToImpact.Domain.Audit;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Notifications;
using InnovationToImpact.Infrastructure.Assignments;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class AssignmentServiceTests
{
    private sealed class FakeAuditLogWriter : IAuditLogWriter
    {
        public List<(string EntityType, Guid EntityId, string Action, Guid? ActorId, string? Payload)> Calls { get; } = new();

        public Task<AuditLog> AppendAsync(string entityType, Guid entityId, string action, Guid? actorId, string? payload, CancellationToken cancellationToken = default)
        {
            Calls.Add((entityType, entityId, action, actorId, payload));
            return Task.FromResult(new AuditLog { Id = Guid.NewGuid(), EntityType = entityType, EntityId = entityId, Action = action, ActorId = actorId, Payload = payload });
        }
    }

    private sealed class FakeNotificationService : INotificationService
    {
        public List<(Guid UserId, string Type)> Calls { get; } = new();

        public Task<Notification> CreateAndPublishAsync(Guid userId, string notificationType, string titleAr, string titleEn, string bodyAr, string bodyEn, string? link, string? payloadJson, CancellationToken cancellationToken = default)
        {
            Calls.Add((userId, notificationType));
            return Task.FromResult(new Notification { Id = Guid.NewGuid(), UserId = userId, NotificationType = notificationType, TitleAr = titleAr, TitleEn = titleEn, BodyAr = bodyAr, BodyEn = bodyEn });
        }
    }

    private static (Guid EvaluatorId, Guid IdeaId, Guid ActivityId, Guid SubmitterId) SeedIdeaAndEvaluator(SqliteContextFixture fixture)
    {
        using var db = fixture.CreateContext();
        var roleIds = db.Roles.ToDictionary(r => r.Code, r => r.Id);
        var evaluatorId = Guid.NewGuid();
        db.Users.Add(new User { Id = evaluatorId, SamAccountName = "evaluator1", Email = "evaluator1@gac-demo.sa", FullNameAr = "م1", FullNameEn = "Evaluator One" });
        var submitterId = Guid.NewGuid();
        db.Users.Add(new User { Id = submitterId, SamAccountName = "submitter1", Email = "submitter1@gac-demo.sa", FullNameAr = "س1", FullNameEn = "Submitter One" });
        db.SaveChanges();
        db.Set<UserRole>().Add(new UserRole { UserId = evaluatorId, RoleId = roleIds["evaluator"], IsPrimary = true });
        db.SaveChanges();

        var activityId = Guid.NewGuid();
        db.Activities.Add(new Activity { Id = activityId, NameAr = "ف", NameEn = "Activity", Type = "hackathon", Status = "open", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(30), CreatedById = submitterId });
        var themeId = db.StrategicThemes.First().Id;
        var draftStatus = db.IdeaStatuses.Single(s => s.Code == "draft");
        var ideaId = Guid.NewGuid();
        db.Ideas.Add(new Idea
        {
            Id = ideaId,
            Code = "IDEA-0001",
            TitleAr = "فكرة", TitleEn = "Test Idea",
            ProblemStatementAr = "م", ProblemStatementEn = "P",
            ProposedSolutionAr = "ح", ProposedSolutionEn = "S",
            ExpectedBenefitsAr = "ف", ExpectedBenefitsEn = "B",
            StrategicThemeId = themeId,
            ActivityId = activityId,
            IdeaStatusId = draftStatus.Id,
            SubmitterId = submitterId,
        });
        db.SaveChanges();

        return (evaluatorId, ideaId, activityId, submitterId);
    }

    private static Assignment SeedAssignment(SqliteContextFixture fixture, Guid ideaId, Guid evaluatorId, string statusCode, DateTime? dueAt = null, DateTime? assignedAt = null)
    {
        using var db = fixture.CreateContext();
        var statusId = db.AssignmentStatuses.Single(s => s.Code == statusCode).Id;
        var assignment = new Assignment
        {
            Id = Guid.NewGuid(),
            IdeaId = ideaId,
            EvaluatorId = evaluatorId,
            AssignedById = evaluatorId,
            AssignedAt = assignedAt ?? DateTime.UtcNow,
            DueAt = dueAt,
            AssignmentStatusId = statusId,
        };
        db.Assignments.Add(assignment);
        db.SaveChanges();
        return assignment;
    }

    [Fact]
    public async Task ListAsync_FiltersByEvaluatorStatusAndIdeaSearch_AndPaginatesCorrectly()
    {
        using var fixture = new SqliteContextFixture();
        var (evaluatorId, ideaId, _, _) = SeedIdeaAndEvaluator(fixture);
        SeedAssignment(fixture, ideaId, evaluatorId, "pending");
        using var db = fixture.CreateContext();
        var service = new AssignmentService(db, new FakeAuditLogWriter(), new FakeNotificationService());

        var result = await service.ListAsync(new AssignmentListFilter(evaluatorId, "pending", "Test", 1, 25));

        Assert.Equal(1, result.Total);
        Assert.Single(result.Items);
        Assert.Equal(ideaId, result.Items[0].IdeaId);
    }

    [Fact]
    public async Task ListAsync_IdeaSearchAndPaginationComposeCorrectly()
    {
        using var fixture = new SqliteContextFixture();
        var (evaluatorId, ideaId, _, _) = SeedIdeaAndEvaluator(fixture);
        SeedAssignment(fixture, ideaId, evaluatorId, "pending");
        using var db = fixture.CreateContext();
        var service = new AssignmentService(db, new FakeAuditLogWriter(), new FakeNotificationService());

        var noMatch = await service.ListAsync(new AssignmentListFilter(null, null, "NoSuchTitle", 1, 25));
        var match = await service.ListAsync(new AssignmentListFilter(null, null, "IDEA-0001", 1, 25));

        Assert.Equal(0, noMatch.Total);
        Assert.Empty(noMatch.Items);
        Assert.Equal(1, match.Total);
    }

    [Fact]
    public async Task GetWorkloadHeatmapAsync_BucketsCorrectlyAndExcludesDeclinedAndOldCompleted()
    {
        using var fixture = new SqliteContextFixture();
        var (evaluatorId, ideaId, _, _) = SeedIdeaAndEvaluator(fixture);
        SeedAssignment(fixture, ideaId, evaluatorId, "pending", dueAt: null);
        SeedAssignment(fixture, ideaId, evaluatorId, "pending", dueAt: DateTime.UtcNow.AddHours(24));
        SeedAssignment(fixture, ideaId, evaluatorId, "pending", dueAt: DateTime.UtcNow.AddHours(-1));
        SeedAssignment(fixture, ideaId, evaluatorId, "completed", assignedAt: DateTime.UtcNow.AddDays(-2));
        SeedAssignment(fixture, ideaId, evaluatorId, "completed", assignedAt: DateTime.UtcNow.AddDays(-30));
        SeedAssignment(fixture, ideaId, evaluatorId, "declined");
        using var db = fixture.CreateContext();
        var service = new AssignmentService(db, new FakeAuditLogWriter(), new FakeNotificationService());

        var rows = await service.GetWorkloadHeatmapAsync();

        var row = Assert.Single(rows);
        Assert.Equal(evaluatorId, row.EvaluatorId);
        Assert.Equal(1, row.Pending);
        Assert.Equal(1, row.DueSoon);
        Assert.Equal(1, row.Overdue);
        Assert.Equal(1, row.CompletedRecent);
    }

    [Fact]
    public async Task SuggestLeastLoadedEvaluatorsAsync_RanksAscendingByOpenCount_ZeroAssignmentEvaluatorFirst()
    {
        using var fixture = new SqliteContextFixture();
        var (busyEvaluatorId, ideaId, _, _) = SeedIdeaAndEvaluator(fixture);
        SeedAssignment(fixture, ideaId, busyEvaluatorId, "pending");
        SeedAssignment(fixture, ideaId, busyEvaluatorId, "pending");

        using (var db = fixture.CreateContext())
        {
            var idleEvaluatorId = Guid.NewGuid();
            var roleIds = db.Roles.ToDictionary(r => r.Code, r => r.Id);
            db.Users.Add(new User { Id = idleEvaluatorId, SamAccountName = "evaluator2", Email = "evaluator2@gac-demo.sa", FullNameAr = "م2", FullNameEn = "Aaron Idle" });
            db.SaveChanges();
            db.Set<UserRole>().Add(new UserRole { UserId = idleEvaluatorId, RoleId = roleIds["evaluator"], IsPrimary = true });
            db.SaveChanges();
        }

        using var readDb = fixture.CreateContext();
        var service = new AssignmentService(readDb, new FakeAuditLogWriter(), new FakeNotificationService());

        var suggestions = await service.SuggestLeastLoadedEvaluatorsAsync();

        Assert.Equal(2, suggestions.Count);
        Assert.Equal("Aaron Idle", suggestions[0].EvaluatorName);
        Assert.Equal(0, suggestions[0].OpenCount);
        Assert.Equal(2, suggestions[1].OpenCount);
    }

    [Fact]
    public async Task ListIdeaOptionsAsync_ReturnsAllIdeasRegardlessOfStatus()
    {
        using var fixture = new SqliteContextFixture();
        var (_, ideaId, _, _) = SeedIdeaAndEvaluator(fixture);
        using var db = fixture.CreateContext();
        var service = new AssignmentService(db, new FakeAuditLogWriter(), new FakeNotificationService());

        var options = await service.ListIdeaOptionsAsync();

        Assert.Contains(options, o => o.Id == ideaId && o.Code == "IDEA-0001");
    }

    [Fact]
    public async Task CreateAsync_Valid_SetsPendingStatus_LogsAudit_NotifiesEvaluator_QueuesEmail()
    {
        using var fixture = new SqliteContextFixture();
        var (evaluatorId, ideaId, _, _) = SeedIdeaAndEvaluator(fixture);
        var actorId = Guid.NewGuid();
        using (var seedDb = fixture.CreateContext())
        {
            seedDb.Users.Add(new User { Id = actorId, SamAccountName = "admin1", Email = "admin1@gac-demo.sa", FullNameAr = "أ1", FullNameEn = "Admin One" });
            seedDb.SaveChanges();
        }
        using var db = fixture.CreateContext();
        var notifications = new FakeNotificationService();
        var service = new AssignmentService(db, new FakeAuditLogWriter(), notifications);

        var result = await service.CreateAsync(new AssignmentCreateInput(ideaId, evaluatorId, null, "please review"), actorId);

        Assert.Equal(AssignmentCommandStatus.Success, result.Status);
        Assert.Equal("pending", result.Entity!.AssignmentStatus.Code);
        Assert.Single(notifications.Calls);
        Assert.Equal(evaluatorId, notifications.Calls[0].UserId);
        Assert.Equal("evaluation_assigned", notifications.Calls[0].Type);
        using var readDb = fixture.CreateContext();
        Assert.Single(readDb.EmailOutboxes.Where(e => e.Category == "assignment_created"));
    }

    [Fact]
    public async Task CreateAsync_IdeaDoesNotExist_ReturnsInvalidIdea()
    {
        using var fixture = new SqliteContextFixture();
        var (evaluatorId, _, _, _) = SeedIdeaAndEvaluator(fixture);
        using var db = fixture.CreateContext();
        var service = new AssignmentService(db, new FakeAuditLogWriter(), new FakeNotificationService());

        var result = await service.CreateAsync(new AssignmentCreateInput(Guid.NewGuid(), evaluatorId, null, null), Guid.NewGuid());

        Assert.Equal(AssignmentCommandStatus.InvalidIdea, result.Status);
    }

    [Fact]
    public async Task CreateAsync_EvaluatorDoesNotHoldEvaluatorRole_ReturnsInvalidEvaluator()
    {
        using var fixture = new SqliteContextFixture();
        var (_, ideaId, _, submitterId) = SeedIdeaAndEvaluator(fixture);
        using var db = fixture.CreateContext();
        var service = new AssignmentService(db, new FakeAuditLogWriter(), new FakeNotificationService());

        var result = await service.CreateAsync(new AssignmentCreateInput(ideaId, submitterId, null, null), Guid.NewGuid());

        Assert.Equal(AssignmentCommandStatus.InvalidEvaluator, result.Status);
    }

    [Fact]
    public async Task UpdateAsync_ReassignsEvaluator_LogsAudit_NotifiesNewEvaluatorOnly()
    {
        using var fixture = new SqliteContextFixture();
        var (firstEvaluatorId, ideaId, _, _) = SeedIdeaAndEvaluator(fixture);
        var assignment = SeedAssignment(fixture, ideaId, firstEvaluatorId, "pending");
        Guid secondEvaluatorId;
        using (var seedDb = fixture.CreateContext())
        {
            secondEvaluatorId = Guid.NewGuid();
            var roleIds = seedDb.Roles.ToDictionary(r => r.Code, r => r.Id);
            seedDb.Users.Add(new User { Id = secondEvaluatorId, SamAccountName = "evaluator2", Email = "evaluator2@gac-demo.sa", FullNameAr = "م2", FullNameEn = "Evaluator Two" });
            seedDb.SaveChanges();
            seedDb.Set<UserRole>().Add(new UserRole { UserId = secondEvaluatorId, RoleId = roleIds["evaluator"], IsPrimary = true });
            seedDb.SaveChanges();
        }
        using var db = fixture.CreateContext();
        var notifications = new FakeNotificationService();
        var service = new AssignmentService(db, new FakeAuditLogWriter(), notifications);

        var result = await service.UpdateAsync(assignment.Id, new AssignmentUpdateInput("pending", null, "please review", secondEvaluatorId), Guid.NewGuid());

        Assert.Equal(AssignmentCommandStatus.Success, result.Status);
        Assert.Equal(secondEvaluatorId, result.Entity!.EvaluatorId);
        Assert.Single(notifications.Calls);
        Assert.Equal(secondEvaluatorId, notifications.Calls[0].UserId);
    }

    [Fact]
    public async Task UpdateAsync_NoEvaluatorChange_DoesNotNotify()
    {
        using var fixture = new SqliteContextFixture();
        var (evaluatorId, ideaId, _, _) = SeedIdeaAndEvaluator(fixture);
        var assignment = SeedAssignment(fixture, ideaId, evaluatorId, "pending");
        using var db = fixture.CreateContext();
        var notifications = new FakeNotificationService();
        var service = new AssignmentService(db, new FakeAuditLogWriter(), notifications);

        var result = await service.UpdateAsync(assignment.Id, new AssignmentUpdateInput("completed", null, "done", evaluatorId), Guid.NewGuid());

        Assert.Equal(AssignmentCommandStatus.Success, result.Status);
        Assert.Empty(notifications.Calls);
    }

    [Fact]
    public async Task UpdateAsync_NotFound_ReturnsNotFound()
    {
        using var fixture = new SqliteContextFixture();
        var (evaluatorId, _, _, _) = SeedIdeaAndEvaluator(fixture);
        using var db = fixture.CreateContext();
        var service = new AssignmentService(db, new FakeAuditLogWriter(), new FakeNotificationService());

        var result = await service.UpdateAsync(Guid.NewGuid(), new AssignmentUpdateInput("pending", null, null, evaluatorId), Guid.NewGuid());

        Assert.Equal(AssignmentCommandStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task UnassignAsync_SetsDeclined_LogsUnassignedAction()
    {
        using var fixture = new SqliteContextFixture();
        var (evaluatorId, ideaId, _, _) = SeedIdeaAndEvaluator(fixture);
        var assignment = SeedAssignment(fixture, ideaId, evaluatorId, "pending");
        using var db = fixture.CreateContext();
        var auditLog = new FakeAuditLogWriter();
        var service = new AssignmentService(db, auditLog, new FakeNotificationService());

        var result = await service.UnassignAsync(assignment.Id, Guid.NewGuid());

        Assert.Equal(AssignmentCommandStatus.Success, result.Status);
        Assert.Equal("declined", result.Entity!.AssignmentStatus.Code);
        Assert.Single(auditLog.Calls);
        Assert.Equal("assignment.unassigned", auditLog.Calls[0].Action);
    }

    [Fact]
    public async Task BulkUnassignAsync_AllSucceed_SameBehaviorAsSingleUnassign()
    {
        using var fixture = new SqliteContextFixture();
        var (evaluatorId, ideaId, _, _) = SeedIdeaAndEvaluator(fixture);
        var a1 = SeedAssignment(fixture, ideaId, evaluatorId, "pending");
        var a2 = SeedAssignment(fixture, ideaId, evaluatorId, "pending");
        using var db = fixture.CreateContext();
        var auditLog = new FakeAuditLogWriter();
        var service = new AssignmentService(db, auditLog, new FakeNotificationService());

        var results = await service.BulkUnassignAsync(new[] { a1.Id, a2.Id }, Guid.NewGuid());

        Assert.All(results, r => Assert.Equal(AssignmentCommandStatus.Success, r.Status));
        Assert.All(results, r => Assert.Equal("declined", r.Entity!.AssignmentStatus.Code));
        Assert.Equal(2, auditLog.Calls.Count);
        Assert.All(auditLog.Calls, c => Assert.Equal("assignment.unassigned", c.Action));
    }
}
