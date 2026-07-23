using System.Text.Json;
using InnovationToImpact.Domain.Assignments;
using InnovationToImpact.Domain.Audit;
using InnovationToImpact.Domain.Email;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Notifications;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Assignments;

public class AssignmentService : IAssignmentService
{
    private static readonly TimeSpan DueSoonWindow = TimeSpan.FromHours(48);
    private static readonly TimeSpan RecentCompletionWindow = TimeSpan.FromDays(7);

    private readonly InnovationDbContext _db;
    private readonly IAuditLogWriter _auditLogWriter;
    private readonly INotificationService _notificationService;

    public AssignmentService(InnovationDbContext db, IAuditLogWriter auditLogWriter, INotificationService notificationService)
    {
        _db = db;
        _auditLogWriter = auditLogWriter;
        _notificationService = notificationService;
    }

    public async Task<AssignmentPageResult> ListAsync(AssignmentListFilter filter, CancellationToken cancellationToken = default)
    {
        var query = _db.Assignments
            .Include(a => a.Idea)
            .Include(a => a.Evaluator)
            .Include(a => a.AssignmentStatus)
            .AsQueryable();

        if (filter.EvaluatorId is not null)
            query = query.Where(a => a.EvaluatorId == filter.EvaluatorId);
        if (!string.IsNullOrWhiteSpace(filter.StatusCode))
            query = query.Where(a => a.AssignmentStatus.Code == filter.StatusCode);
        if (!string.IsNullOrWhiteSpace(filter.IdeaSearch))
            query = query.Where(a => a.Idea.Code.Contains(filter.IdeaSearch) || a.Idea.TitleAr.Contains(filter.IdeaSearch) || a.Idea.TitleEn.Contains(filter.IdeaSearch));

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(a => a.AssignedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(cancellationToken);

        return new AssignmentPageResult(items, total, filter.Page, filter.PageSize);
    }

    public async Task<IReadOnlyList<WorkloadRow>> GetWorkloadHeatmapAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var assignments = await _db.Assignments
            .Include(a => a.Evaluator)
            .Include(a => a.AssignmentStatus)
            .Where(a => a.AssignmentStatus.Code != "declined")
            .ToListAsync(cancellationToken);

        var rows = new List<WorkloadRow>();
        foreach (var group in assignments.GroupBy(a => a.EvaluatorId))
        {
            var pending = 0;
            var dueSoon = 0;
            var overdue = 0;
            var completedRecent = 0;
            string evaluatorName = group.First().Evaluator.FullNameEn;

            foreach (var a in group)
            {
                if (a.AssignmentStatus.Code == "completed")
                {
                    if (now - a.AssignedAt <= RecentCompletionWindow) completedRecent++;
                    continue;
                }
                if (a.DueAt is null) { pending++; continue; }
                if (a.DueAt.Value < now) { overdue++; continue; }
                if (a.DueAt.Value - now <= DueSoonWindow) { dueSoon++; continue; }
                pending++;
            }

            rows.Add(new WorkloadRow(group.Key, evaluatorName, pending, dueSoon, overdue, completedRecent));
        }

        return rows;
    }

    public async Task<IReadOnlyList<SuggestedEvaluator>> SuggestLeastLoadedEvaluatorsAsync(CancellationToken cancellationToken = default)
    {
        var evaluatorRoleId = await _db.Roles.Where(r => r.Code == "evaluator").Select(r => r.Id).SingleAsync(cancellationToken);
        var evaluators = await _db.Users
            .Where(u => u.UserRoles.Any(ur => ur.RoleId == evaluatorRoleId))
            .Select(u => new { u.Id, u.FullNameEn })
            .ToListAsync(cancellationToken);

        var heatmap = await GetWorkloadHeatmapAsync(cancellationToken);
        var openCountByEvaluator = heatmap.ToDictionary(r => r.EvaluatorId, r => r.Pending + r.DueSoon + r.Overdue);

        return evaluators
            .Select(e => new SuggestedEvaluator(e.Id, e.FullNameEn, openCountByEvaluator.GetValueOrDefault(e.Id, 0)))
            .OrderBy(s => s.OpenCount)
            .ThenBy(s => s.EvaluatorName, StringComparer.Ordinal)
            .Take(3)
            .ToList();
    }

    public async Task<IReadOnlyList<IdeaOption>> ListIdeaOptionsAsync(CancellationToken cancellationToken = default) =>
        await _db.Ideas
            .OrderBy(i => i.Code)
            .Select(i => new IdeaOption(i.Id, i.Code, i.TitleAr, i.TitleEn))
            .ToListAsync(cancellationToken);

    public async Task<AssignmentCommandResult> CreateAsync(AssignmentCreateInput input, Guid actorId, CancellationToken cancellationToken = default)
    {
        var idea = await _db.Ideas.SingleOrDefaultAsync(i => i.Id == input.IdeaId, cancellationToken);
        if (idea is null) return new AssignmentCommandResult(AssignmentCommandStatus.InvalidIdea);

        var evaluatorRoleId = await _db.Roles.Where(r => r.Code == "evaluator").Select(r => r.Id).SingleAsync(cancellationToken);
        var evaluatorExists = await _db.Users.AnyAsync(u => u.Id == input.EvaluatorId && u.UserRoles.Any(ur => ur.RoleId == evaluatorRoleId), cancellationToken);
        if (!evaluatorExists) return new AssignmentCommandResult(AssignmentCommandStatus.InvalidEvaluator);

        var pendingStatusId = await _db.AssignmentStatuses.Where(s => s.Code == "pending").Select(s => s.Id).SingleAsync(cancellationToken);
        var assignment = new Assignment
        {
            Id = Guid.NewGuid(),
            IdeaId = input.IdeaId,
            EvaluatorId = input.EvaluatorId,
            AssignedById = actorId,
            AssignedAt = DateTime.UtcNow,
            DueAt = input.DueAt,
            AssignmentStatusId = pendingStatusId,
            Notes = input.Notes,
        };
        _db.Assignments.Add(assignment);
        await _db.SaveChangesAsync(cancellationToken);
        await _auditLogWriter.AppendAsync("assignment", assignment.Id, "assignment.created", actorId, JsonSerializer.Serialize(input), cancellationToken);
        await NotifyEvaluatorAsync(assignment.Id, input.EvaluatorId, cancellationToken);

        await _db.Entry(assignment).Reference(a => a.AssignmentStatus).LoadAsync(cancellationToken);
        return new AssignmentCommandResult(AssignmentCommandStatus.Success, assignment);
    }

    public async Task<IReadOnlyList<AssignmentCommandResult>> BulkCreateAsync(IReadOnlyList<AssignmentCreateInput> inputs, Guid actorId, CancellationToken cancellationToken = default)
    {
        var results = new List<AssignmentCommandResult>();
        foreach (var input in inputs)
        {
            results.Add(await CreateAsync(input, actorId, cancellationToken));
        }
        return results;
    }

    public async Task<AssignmentCommandResult> UpdateAsync(Guid id, AssignmentUpdateInput input, Guid actorId, CancellationToken cancellationToken = default)
    {
        var assignment = await _db.Assignments.Include(a => a.AssignmentStatus).SingleOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (assignment is null) return new AssignmentCommandResult(AssignmentCommandStatus.NotFound);

        var before = new { assignment.AssignmentStatus.Code, assignment.DueAt, assignment.Notes, assignment.EvaluatorId };
        var isReassignment = assignment.EvaluatorId != input.EvaluatorId;

        var statusId = await _db.AssignmentStatuses.Where(s => s.Code == input.StatusCode).Select(s => s.Id).SingleAsync(cancellationToken);
        assignment.AssignmentStatusId = statusId;
        assignment.DueAt = input.DueAt;
        assignment.Notes = input.Notes;
        assignment.EvaluatorId = input.EvaluatorId;
        await _db.SaveChangesAsync(cancellationToken);
        await _db.Entry(assignment).Reference(a => a.AssignmentStatus).LoadAsync(cancellationToken);

        await _auditLogWriter.AppendAsync("assignment", assignment.Id, "assignment.updated", actorId,
            JsonSerializer.Serialize(new { before, after = input }), cancellationToken);

        if (isReassignment)
        {
            await NotifyEvaluatorAsync(assignment.Id, input.EvaluatorId, cancellationToken);
        }

        return new AssignmentCommandResult(AssignmentCommandStatus.Success, assignment);
    }

    public async Task<AssignmentCommandResult> UnassignAsync(Guid id, Guid actorId, CancellationToken cancellationToken = default)
    {
        var assignment = await _db.Assignments.Include(a => a.AssignmentStatus).SingleOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (assignment is null) return new AssignmentCommandResult(AssignmentCommandStatus.NotFound);

        var declinedStatusId = await _db.AssignmentStatuses.Where(s => s.Code == "declined").Select(s => s.Id).SingleAsync(cancellationToken);
        assignment.AssignmentStatusId = declinedStatusId;
        await _db.SaveChangesAsync(cancellationToken);
        await _db.Entry(assignment).Reference(a => a.AssignmentStatus).LoadAsync(cancellationToken);
        await _auditLogWriter.AppendAsync("assignment", assignment.Id, "assignment.unassigned", actorId, null, cancellationToken);

        return new AssignmentCommandResult(AssignmentCommandStatus.Success, assignment);
    }

    public async Task<IReadOnlyList<AssignmentCommandResult>> BulkUnassignAsync(IReadOnlyList<Guid> ids, Guid actorId, CancellationToken cancellationToken = default)
    {
        var results = new List<AssignmentCommandResult>();
        foreach (var id in ids)
        {
            results.Add(await UnassignAsync(id, actorId, cancellationToken));
        }
        return results;
    }

    private async Task NotifyEvaluatorAsync(Guid assignmentId, Guid evaluatorId, CancellationToken cancellationToken)
    {
        await _notificationService.CreateAndPublishAsync(
            evaluatorId,
            "evaluation_assigned",
            titleAr: "تم إسناد فكرة لك للتقييم",
            titleEn: "An idea has been assigned to you for evaluation",
            bodyAr: "تم إسناد فكرة جديدة إليك للتقييم. يرجى مراجعتها في أقرب وقت ممكن.",
            bodyEn: "A new idea has been assigned to you for evaluation. Please review it as soon as possible.",
            link: "/evaluations/queue",
            payloadJson: JsonSerializer.Serialize(new { assignmentId }),
            cancellationToken);

        var evaluator = await _db.Users.SingleAsync(u => u.Id == evaluatorId, cancellationToken);
        var emailPendingStatus = await _db.EmailOutboxStatuses.SingleAsync(s => s.Code == EmailOutboxStatusCodes.Pending, cancellationToken);
        _db.EmailOutboxes.Add(new EmailOutbox
        {
            Id = Guid.NewGuid(),
            ToEmail = evaluator.Email,
            ToUserId = evaluator.Id,
            Subject = "An idea has been assigned to you for evaluation",
            BodyHtml = "<p>A new idea has been assigned to you for evaluation. Please review it in your evaluation queue.</p>",
            Category = "assignment_created",
            EmailOutboxStatusId = emailPendingStatus.Id,
            Attempts = 0,
        });
        await _db.SaveChangesAsync(cancellationToken);
    }
}
