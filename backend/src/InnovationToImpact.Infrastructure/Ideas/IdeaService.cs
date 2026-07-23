using InnovationToImpact.Domain.Audit;
using InnovationToImpact.Domain.Auth;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Ideas;
using InnovationToImpact.Domain.Notifications;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Linq;

namespace InnovationToImpact.Infrastructure.Ideas;

public class IdeaService : IIdeaService
{
    private readonly InnovationDbContext _db;
    private readonly IEvidenceFileStorage _storage;
    private readonly IAuditLogWriter _auditLogWriter;
    private readonly INotificationService _notificationService;

    public IdeaService(InnovationDbContext db, IEvidenceFileStorage storage, IAuditLogWriter auditLogWriter, INotificationService notificationService)
    {
        _db = db;
        _storage = storage;
        _auditLogWriter = auditLogWriter;
        _notificationService = notificationService;
    }

    private static readonly Regex EmailRegex = new(@"\S+@\S+\.\S+", RegexOptions.Compiled);

    private async Task<IdeaCommandStatus?> ValidateNewFieldsAsync(IdeaInput input, CancellationToken cancellationToken)
    {
        var activityExists = await _db.Activities.AnyAsync(a => a.Id == input.ActivityId, cancellationToken);
        if (!activityExists) return IdeaCommandStatus.InvalidActivity;

        if (input.ChallengeId is Guid challengeId)
        {
            var challengeValid = await _db.Challenges.AnyAsync(c => c.Id == challengeId && c.StrategicThemeId == input.StrategicThemeId && c.IsActive, cancellationToken);
            if (!challengeValid) return IdeaCommandStatus.InvalidChallenge;
        }
        else
        {
            var hasActiveChallenges = await _db.Challenges.AnyAsync(c => c.StrategicThemeId == input.StrategicThemeId && c.IsActive, cancellationToken);
            if (hasActiveChallenges) return IdeaCommandStatus.InvalidChallenge;
        }

        if (input.ParticipationType != "individual" && input.ParticipationType != "team") return IdeaCommandStatus.InvalidParticipation;
        if (input.ParticipationType == "team")
        {
            if (string.IsNullOrWhiteSpace(input.TeamName)) return IdeaCommandStatus.InvalidParticipation;
            if (input.TeamMembers.Count < 2 || input.TeamMembers.Count > 4) return IdeaCommandStatus.InvalidParticipation;
            foreach (var member in input.TeamMembers)
            {
                if (string.IsNullOrWhiteSpace(member.Name)) return IdeaCommandStatus.InvalidParticipation;
                if (!EmailRegex.IsMatch(member.Email)) return IdeaCommandStatus.InvalidParticipation;
            }
        }

        if (!input.IpAcknowledged || !input.TermsAgreed) return IdeaCommandStatus.ConsentRequired;

        return null;
    }

    private static void ReplaceTeamMembers(InnovationDbContext db, Guid ideaId, IdeaInput input)
    {
        var existing = db.IdeaTeamMembers.Where(m => m.IdeaId == ideaId);
        db.IdeaTeamMembers.RemoveRange(existing);
        if (input.ParticipationType != "team") return;
        var index = 0;
        foreach (var member in input.TeamMembers)
        {
            db.IdeaTeamMembers.Add(new IdeaTeamMember { Id = Guid.NewGuid(), IdeaId = ideaId, Name = member.Name, Email = member.Email, SortOrder = index });
            index++;
        }
    }

    public async Task<IdeaQueryResult> CreateAsync(Guid submitterId, IdeaInput input, CancellationToken cancellationToken = default)
    {
        var themeExists = await _db.StrategicThemes.AnyAsync(t => t.Id == input.StrategicThemeId, cancellationToken);
        if (!themeExists) return new IdeaQueryResult(IdeaCommandStatus.InvalidStrategicTheme);

        var validationError = await ValidateNewFieldsAsync(input, cancellationToken);
        if (validationError is not null) return new IdeaQueryResult(validationError.Value);

        var draftStatus = await _db.IdeaStatuses.SingleAsync(s => s.Code == IdeaStatusCodes.Draft, cancellationToken);
        var code = await GenerateNextCodeAsync(cancellationToken);

        var idea = new Idea
        {
            Id = Guid.NewGuid(),
            Code = code,
            TitleAr = input.TitleAr,
            TitleEn = input.TitleEn,
            ProblemStatementAr = input.ProblemStatementAr,
            ProblemStatementEn = input.ProblemStatementEn,
            ProposedSolutionAr = input.ProposedSolutionAr,
            ProposedSolutionEn = input.ProposedSolutionEn,
            ExpectedBenefitsAr = input.ExpectedBenefitsAr,
            ExpectedBenefitsEn = input.ExpectedBenefitsEn,
            StrategicThemeId = input.StrategicThemeId,
            ActivityId = input.ActivityId,
            ChallengeId = input.ChallengeId,
            ParticipationType = input.ParticipationType,
            TeamName = input.ParticipationType == "team" ? input.TeamName : null,
            IpAcknowledged = input.IpAcknowledged,
            TermsAgreed = input.TermsAgreed,
            IdeaStatusId = draftStatus.Id,
            CurrentStage = 0,
            SubmitterId = submitterId,
        };

        _db.Ideas.Add(idea);
        ReplaceTeamMembers(_db, idea.Id, input);
        await _db.SaveChangesAsync(cancellationToken);

        return new IdeaQueryResult(IdeaCommandStatus.Success, idea);
    }

    public async Task<IdeaQueryResult> UpdateAsync(Guid ideaId, Guid submitterId, IdeaInput input, CancellationToken cancellationToken = default)
    {
        var idea = await _db.Ideas.Include(i => i.IdeaStatus).SingleOrDefaultAsync(i => i.Id == ideaId, cancellationToken);
        if (idea is null) return new IdeaQueryResult(IdeaCommandStatus.NotFound);
        if (idea.SubmitterId != submitterId) return new IdeaQueryResult(IdeaCommandStatus.Forbidden);
        if (idea.IdeaStatus.Code != IdeaStatusCodes.Draft && idea.IdeaStatus.Code != IdeaStatusCodes.Returned) return new IdeaQueryResult(IdeaCommandStatus.InvalidState);

        var themeExists = await _db.StrategicThemes.AnyAsync(t => t.Id == input.StrategicThemeId, cancellationToken);
        if (!themeExists) return new IdeaQueryResult(IdeaCommandStatus.InvalidStrategicTheme);

        var validationError = await ValidateNewFieldsAsync(input, cancellationToken);
        if (validationError is not null) return new IdeaQueryResult(validationError.Value);

        idea.TitleAr = input.TitleAr;
        idea.TitleEn = input.TitleEn;
        idea.ProblemStatementAr = input.ProblemStatementAr;
        idea.ProblemStatementEn = input.ProblemStatementEn;
        idea.ProposedSolutionAr = input.ProposedSolutionAr;
        idea.ProposedSolutionEn = input.ProposedSolutionEn;
        idea.ExpectedBenefitsAr = input.ExpectedBenefitsAr;
        idea.ExpectedBenefitsEn = input.ExpectedBenefitsEn;
        idea.StrategicThemeId = input.StrategicThemeId;
        idea.ActivityId = input.ActivityId;
        idea.ChallengeId = input.ChallengeId;
        idea.ParticipationType = input.ParticipationType;
        idea.TeamName = input.ParticipationType == "team" ? input.TeamName : null;
        idea.IpAcknowledged = input.IpAcknowledged;
        idea.TermsAgreed = input.TermsAgreed;
        ReplaceTeamMembers(_db, idea.Id, input);
        idea.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return new IdeaQueryResult(IdeaCommandStatus.Success, idea);
    }

    public async Task<IdeaQueryResult> SubmitAsync(Guid ideaId, Guid submitterId, CancellationToken cancellationToken = default)
    {
        var idea = await _db.Ideas.Include(i => i.IdeaStatus).SingleOrDefaultAsync(i => i.Id == ideaId, cancellationToken);
        if (idea is null) return new IdeaQueryResult(IdeaCommandStatus.NotFound);
        if (idea.SubmitterId != submitterId) return new IdeaQueryResult(IdeaCommandStatus.Forbidden);
        if (idea.IdeaStatus.Code != IdeaStatusCodes.Draft && idea.IdeaStatus.Code != IdeaStatusCodes.Returned) return new IdeaQueryResult(IdeaCommandStatus.InvalidState);

        var attachmentCount = await _db.EvidenceAttachments.CountAsync(
            a => a.EntityType == EvidenceEntityTypes.Idea && a.EntityId == ideaId && a.DeletedAt == null,
            cancellationToken);
        if (attachmentCount == 0) return new IdeaQueryResult(IdeaCommandStatus.InvalidState);

        var submittedStatus = await _db.IdeaStatuses.SingleAsync(s => s.Code == IdeaStatusCodes.Submitted, cancellationToken);
        idea.IdeaStatusId = submittedStatus.Id;
        idea.IdeaStatus = submittedStatus;
        idea.CurrentStage = 1;
        idea.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return new IdeaQueryResult(IdeaCommandStatus.Success, idea);
    }

    public async Task<IdeaQueryResult> SubmitToCommitteeAsync(Guid ideaId, Guid submitterId, CancellationToken cancellationToken = default)
    {
        var idea = await _db.Ideas.Include(i => i.IdeaStatus).SingleOrDefaultAsync(i => i.Id == ideaId, cancellationToken);
        if (idea is null) return new IdeaQueryResult(IdeaCommandStatus.NotFound);
        if (idea.SubmitterId != submitterId) return new IdeaQueryResult(IdeaCommandStatus.Forbidden);
        if (idea.IdeaStatus.Code != IdeaStatusCodes.PassAwaitingAttachments) return new IdeaQueryResult(IdeaCommandStatus.InvalidState);

        var attachmentCount = await _db.EvidenceAttachments.CountAsync(
            a => a.EntityType == EvidenceEntityTypes.Idea && a.EntityId == ideaId && a.DeletedAt == null,
            cancellationToken);
        if (attachmentCount == 0) return new IdeaQueryResult(IdeaCommandStatus.InvalidState);

        var committeeStatus = await _db.IdeaStatuses.SingleAsync(s => s.Code == IdeaStatusCodes.Committee, cancellationToken);
        idea.IdeaStatusId = committeeStatus.Id;
        idea.IdeaStatus = committeeStatus;
        idea.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        return new IdeaQueryResult(IdeaCommandStatus.Success, idea);
    }

    public async Task<IdeaQueryResult> ResubmitAsync(Guid ideaId, Guid submitterId, IdeaResubmitInput input, CancellationToken cancellationToken = default)
    {
        var idea = await _db.Ideas.Include(i => i.IdeaStatus).SingleOrDefaultAsync(i => i.Id == ideaId, cancellationToken);
        if (idea is null) return new IdeaQueryResult(IdeaCommandStatus.NotFound);
        if (idea.SubmitterId != submitterId) return new IdeaQueryResult(IdeaCommandStatus.Forbidden);
        if (idea.IdeaStatus.Code != IdeaStatusCodes.Returned) return new IdeaQueryResult(IdeaCommandStatus.InvalidState);

        var allowed = string.IsNullOrWhiteSpace(idea.EditableSections)
            ? IdeaSectionKeys.All
            : idea.EditableSections.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet();

        if (!allowed.Contains("title") && (input.TitleAr != idea.TitleAr || input.TitleEn != idea.TitleEn)) return new IdeaQueryResult(IdeaCommandStatus.SectionNotEditable);
        if (!allowed.Contains("proposed_solution") && (input.ProposedSolutionAr != idea.ProposedSolutionAr || input.ProposedSolutionEn != idea.ProposedSolutionEn)) return new IdeaQueryResult(IdeaCommandStatus.SectionNotEditable);
        if (!allowed.Contains("activity_id") && input.ActivityId != idea.ActivityId) return new IdeaQueryResult(IdeaCommandStatus.SectionNotEditable);
        if (!allowed.Contains("strategic_theme_id") && input.StrategicThemeId != idea.StrategicThemeId) return new IdeaQueryResult(IdeaCommandStatus.SectionNotEditable);
        if (!allowed.Contains("challenge") && input.ChallengeId != idea.ChallengeId) return new IdeaQueryResult(IdeaCommandStatus.SectionNotEditable);
        if (!allowed.Contains("participation_type") && input.ParticipationType != idea.ParticipationType) return new IdeaQueryResult(IdeaCommandStatus.SectionNotEditable);
        if (!allowed.Contains("team") && input.TeamName != idea.TeamName) return new IdeaQueryResult(IdeaCommandStatus.SectionNotEditable);

        var themeExists = await _db.StrategicThemes.AnyAsync(t => t.Id == input.StrategicThemeId, cancellationToken);
        if (!themeExists) return new IdeaQueryResult(IdeaCommandStatus.InvalidStrategicTheme);

        var activityExists = await _db.Activities.AnyAsync(a => a.Id == input.ActivityId, cancellationToken);
        if (!activityExists) return new IdeaQueryResult(IdeaCommandStatus.InvalidActivity);

        if (input.ChallengeId is Guid challengeId)
        {
            var challengeValid = await _db.Challenges.AnyAsync(c => c.Id == challengeId && c.StrategicThemeId == input.StrategicThemeId && c.IsActive, cancellationToken);
            if (!challengeValid) return new IdeaQueryResult(IdeaCommandStatus.InvalidChallenge);
        }
        else
        {
            var hasActiveChallenges = await _db.Challenges.AnyAsync(c => c.StrategicThemeId == input.StrategicThemeId && c.IsActive, cancellationToken);
            if (hasActiveChallenges) return new IdeaQueryResult(IdeaCommandStatus.InvalidChallenge);
        }

        if (input.ParticipationType != "individual" && input.ParticipationType != "team") return new IdeaQueryResult(IdeaCommandStatus.InvalidParticipation);
        if (input.ParticipationType == "team")
        {
            if (string.IsNullOrWhiteSpace(input.TeamName)) return new IdeaQueryResult(IdeaCommandStatus.InvalidParticipation);
            if (input.TeamMembers.Count < 2 || input.TeamMembers.Count > 4) return new IdeaQueryResult(IdeaCommandStatus.InvalidParticipation);
            foreach (var member in input.TeamMembers)
            {
                if (string.IsNullOrWhiteSpace(member.Name)) return new IdeaQueryResult(IdeaCommandStatus.InvalidParticipation);
                if (!EmailRegex.IsMatch(member.Email)) return new IdeaQueryResult(IdeaCommandStatus.InvalidParticipation);
            }
        }

        idea.TitleAr = input.TitleAr;
        idea.TitleEn = input.TitleEn;
        idea.ProposedSolutionAr = input.ProposedSolutionAr;
        idea.ProposedSolutionEn = input.ProposedSolutionEn;
        idea.ActivityId = input.ActivityId;
        idea.StrategicThemeId = input.StrategicThemeId;
        idea.ChallengeId = input.ChallengeId;
        idea.ParticipationType = input.ParticipationType;
        idea.TeamName = input.ParticipationType == "team" ? input.TeamName : null;

        if (allowed.Contains("team"))
        {
            var existing = _db.IdeaTeamMembers.Where(m => m.IdeaId == idea.Id);
            _db.IdeaTeamMembers.RemoveRange(existing);
            if (input.ParticipationType == "team")
            {
                var index = 0;
                foreach (var member in input.TeamMembers)
                {
                    _db.IdeaTeamMembers.Add(new IdeaTeamMember { Id = Guid.NewGuid(), IdeaId = idea.Id, Name = member.Name, Email = member.Email, SortOrder = index });
                    index++;
                }
            }
        }

        var submittedStatus = await _db.IdeaStatuses.SingleAsync(s => s.Code == IdeaStatusCodes.Submitted, cancellationToken);
        idea.IdeaStatusId = submittedStatus.Id;
        idea.IdeaStatus = submittedStatus;
        idea.CurrentStage = 1;
        idea.EditableSections = null;
        idea.ScreeningReason = null;
        idea.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return new IdeaQueryResult(IdeaCommandStatus.Success, idea);
    }

    private static readonly string[] WithdrawableStatuses =
        { IdeaStatusCodes.Draft, IdeaStatusCodes.Submitted, IdeaStatusCodes.Returned };

    public async Task<IdeaQueryResult> WithdrawAsync(Guid ideaId, Guid submitterId, CancellationToken cancellationToken = default)
    {
        var idea = await _db.Ideas.Include(i => i.IdeaStatus).SingleOrDefaultAsync(i => i.Id == ideaId, cancellationToken);
        if (idea is null) return new IdeaQueryResult(IdeaCommandStatus.NotFound);
        if (idea.SubmitterId != submitterId) return new IdeaQueryResult(IdeaCommandStatus.Forbidden);
        if (idea.IdeaStatus.Code == IdeaStatusCodes.Withdrawn || !WithdrawableStatuses.Contains(idea.IdeaStatus.Code))
            return new IdeaQueryResult(IdeaCommandStatus.InvalidState);

        var beforeStatus = idea.IdeaStatus.Code;
        var withdrawnStatus = await _db.IdeaStatuses.SingleAsync(s => s.Code == IdeaStatusCodes.Withdrawn, cancellationToken);
        idea.IdeaStatusId = withdrawnStatus.Id;
        idea.IdeaStatus = withdrawnStatus;
        idea.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        await _auditLogWriter.AppendAsync("idea", idea.Id, "idea.withdrawn", submitterId,
            JsonSerializer.Serialize(new { before = beforeStatus, after = IdeaStatusCodes.Withdrawn }), cancellationToken);

        var evaluatorIds = await _db.Assignments.Where(a => a.IdeaId == ideaId).Select(a => a.EvaluatorId).Distinct().ToListAsync(cancellationToken);
        foreach (var evaluatorId in evaluatorIds)
            await _notificationService.CreateAndPublishAsync(evaluatorId, "idea_withdrawn",
                "تم سحب الفكرة", "Idea withdrawn", "قام مقدّم الفكرة بسحبها.", "The submitter withdrew their idea.",
                $"/ideas/{ideaId}", null, cancellationToken);

        return new IdeaQueryResult(IdeaCommandStatus.Success, idea);
    }

    private static readonly Dictionary<string, string[]> StatusGroups = new()
    {
        ["in_review"] = new[] { IdeaStatusCodes.Submitted, IdeaStatusCodes.Evaluation, IdeaStatusCodes.PassAwaitingAttachments, IdeaStatusCodes.Committee, IdeaStatusCodes.PendingFinalRanking },
        ["approved"] = new[] { IdeaStatusCodes.Approved, IdeaStatusCodes.InPilot, IdeaStatusCodes.InMeasurement, IdeaStatusCodes.InScaling },
        ["returned"] = new[] { IdeaStatusCodes.Returned },
    };

    public async Task<IReadOnlyList<MyIdeaItem>> GetMineDetailedAsync(Guid submitterId, string? statusGroup, CancellationToken cancellationToken = default)
    {
        var query = _db.Ideas.Include(i => i.IdeaStatus).Where(i => i.SubmitterId == submitterId);
        if (!string.IsNullOrWhiteSpace(statusGroup) && StatusGroups.TryGetValue(statusGroup, out var codes))
            query = query.Where(i => codes.Contains(i.IdeaStatus.Code));

        var ideas = await query.OrderByDescending(i => i.UpdatedAt)
            .Select(i => new { i.Id, i.Code, i.TitleAr, i.TitleEn, Status = i.IdeaStatus.Code, i.CurrentStage, i.CreatedAt, i.UpdatedAt })
            .ToListAsync(cancellationToken);

        var ideaIds = ideas.Select(x => x.Id).ToList();
        // Feedback = evaluations + committee decisions with non-empty comments, grouped by idea.
        // NOTE: SQLite's EF provider does not translate string.Trim() in a query predicate (throws
        // NotSupportedException), so non-empty is checked in SQL (!= null && != "") and whitespace-only
        // comments are excluded by filtering client-side after materializing.
        var evalComments = await _db.Evaluations
            .Where(e => ideaIds.Contains(e.IdeaId) && e.Comments != null && e.Comments != "")
            .Select(e => new { e.IdeaId, e.Comments })
            .ToListAsync(cancellationToken);
        var decisionComments = await _db.CommitteeDecisions
            .Where(d => ideaIds.Contains(d.IdeaId) && d.Comments != null && d.Comments != "")
            .Select(d => new { d.IdeaId, d.Comments })
            .ToListAsync(cancellationToken);

        var evalCounts = evalComments.Where(e => !string.IsNullOrWhiteSpace(e.Comments))
            .GroupBy(e => e.IdeaId).ToDictionary(g => g.Key, g => g.Count());
        var decisionCounts = decisionComments.Where(d => !string.IsNullOrWhiteSpace(d.Comments))
            .GroupBy(d => d.IdeaId).ToDictionary(g => g.Key, g => g.Count());

        var feedback = ideaIds.ToDictionary(id => id, id =>
            (evalCounts.TryGetValue(id, out var ec) ? ec : 0) + (decisionCounts.TryGetValue(id, out var dc) ? dc : 0));

        return ideas.Select(i => new MyIdeaItem(i.Id, i.Code, i.TitleAr, i.TitleEn, i.Status, i.CurrentStage, i.CreatedAt, i.UpdatedAt, feedback[i.Id])).ToList();
    }

    public async Task<IdeaQueryResult> GetByIdAsync(Guid ideaId, Guid submitterId, bool isElevatedReviewer = false, CancellationToken cancellationToken = default)
    {
        var idea = await _db.Ideas.Include(i => i.IdeaStatus).Include(i => i.TeamMembers).SingleOrDefaultAsync(i => i.Id == ideaId, cancellationToken);
        if (idea is null) return new IdeaQueryResult(IdeaCommandStatus.NotFound);
        if (idea.SubmitterId != submitterId && !isElevatedReviewer) return new IdeaQueryResult(IdeaCommandStatus.Forbidden);
        return new IdeaQueryResult(IdeaCommandStatus.Success, idea);
    }

    public async Task<IdeaAttachmentResult> AddAttachmentAsync(Guid ideaId, Guid submitterId, string fileName, string contentType, byte[] content, CancellationToken cancellationToken = default)
    {
        var idea = await _db.Ideas.Include(i => i.IdeaStatus).SingleOrDefaultAsync(i => i.Id == ideaId, cancellationToken);
        if (idea is null) return new IdeaAttachmentResult(IdeaCommandStatus.NotFound);
        if (idea.SubmitterId != submitterId) return new IdeaAttachmentResult(IdeaCommandStatus.Forbidden);
        if (idea.IdeaStatus.Code != IdeaStatusCodes.Draft && idea.IdeaStatus.Code != IdeaStatusCodes.PassAwaitingAttachments)
        {
            return new IdeaAttachmentResult(IdeaCommandStatus.InvalidState);
        }
        if (!IdeaAttachmentRules.AllowedContentTypes.Contains(contentType)) return new IdeaAttachmentResult(IdeaCommandStatus.InvalidAttachment);
        if (content.LongLength == 0 || content.LongLength > IdeaAttachmentRules.MaxSizeBytes) return new IdeaAttachmentResult(IdeaCommandStatus.InvalidAttachment);

        var blobPath = await _storage.SaveAsync(fileName, content, cancellationToken);

        var attachment = new EvidenceAttachment
        {
            Id = Guid.NewGuid(),
            EntityType = EvidenceEntityTypes.Idea,
            EntityId = ideaId,
            UploaderId = submitterId,
            FileName = fileName,
            BlobPath = blobPath,
            ContentType = contentType,
            FileSizeBytes = content.LongLength,
        };

        _db.EvidenceAttachments.Add(attachment);
        await _db.SaveChangesAsync(cancellationToken);

        return new IdeaAttachmentResult(IdeaCommandStatus.Success, attachment);
    }

    public async Task<IdeaAttachmentsResult> GetAttachmentsAsync(Guid ideaId, Guid submitterId, bool isElevatedReviewer = false, CancellationToken cancellationToken = default)
    {
        var idea = await _db.Ideas.SingleOrDefaultAsync(i => i.Id == ideaId, cancellationToken);
        if (idea is null) return new IdeaAttachmentsResult(IdeaCommandStatus.NotFound, Array.Empty<EvidenceAttachment>());
        if (idea.SubmitterId != submitterId && !isElevatedReviewer) return new IdeaAttachmentsResult(IdeaCommandStatus.Forbidden, Array.Empty<EvidenceAttachment>());

        var attachments = await _db.EvidenceAttachments
            .Where(a => a.EntityType == EvidenceEntityTypes.Idea && a.EntityId == ideaId && a.DeletedAt == null)
            .OrderBy(a => a.UploadedAt)
            .ToListAsync(cancellationToken);

        return new IdeaAttachmentsResult(IdeaCommandStatus.Success, attachments);
    }

    private static readonly string[] JudgeFinalistStatuses =
    {
        IdeaStatusCodes.Committee, IdeaStatusCodes.Approved,
        IdeaStatusCodes.InPilot, IdeaStatusCodes.InMeasurement, IdeaStatusCodes.InScaling,
    };

    public async Task<IdeaListPage> ListAsync(IdeaListFilter filter, Guid userId, string userEmail, IReadOnlyCollection<string> roles, CancellationToken cancellationToken = default)
    {
        var query = _db.Ideas.Include(i => i.IdeaStatus).AsQueryable();

        // Server-side role scoping (highest privilege among the caller's roles wins).
        if (roles.Contains(RoleCodes.Admin) || roles.Contains(RoleCodes.Supervisor))
        {
            // all ideas
        }
        else if (roles.Contains(RoleCodes.Judge))
        {
            query = query.Where(i => JudgeFinalistStatuses.Contains(i.IdeaStatus.Code));
        }
        else if (roles.Contains(RoleCodes.Evaluator))
        {
            var assignedIds = _db.Assignments.Where(a => a.EvaluatorId == userId).Select(a => a.IdeaId);
            query = query.Where(i => assignedIds.Contains(i.Id));
        }
        else // submitter (and any lesser role)
        {
            query = query.Where(i => i.SubmitterId == userId ||
                (!string.IsNullOrEmpty(userEmail) && i.TeamMembers.Any(tm => tm.Email == userEmail)));
        }

        if (filter.StrategicThemeId is not null) query = query.Where(i => i.StrategicThemeId == filter.StrategicThemeId);
        if (filter.ActivityId is not null) query = query.Where(i => i.ActivityId == filter.ActivityId);
        if (!string.IsNullOrWhiteSpace(filter.Status)) query = query.Where(i => i.IdeaStatus.Code == filter.Status);
        if (filter.Stage is not null) query = query.Where(i => i.CurrentStage == filter.Stage);
        if (!string.IsNullOrWhiteSpace(filter.Q))
        {
            var q = filter.Q;
            query = query.Where(i => i.Code.Contains(q) || i.TitleAr.Contains(q) || i.TitleEn.Contains(q));
        }

        var total = await query.CountAsync(cancellationToken);
        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize is < 1 or > 100 ? 25 : filter.PageSize;
        var items = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(i => new IdeaListItem(i.Id, i.Code, i.TitleAr, i.TitleEn, i.ProblemStatementAr, i.ProblemStatementEn, i.CurrentStage, i.IdeaStatus.Code, i.StrategicThemeId, i.ActivityId))
            .ToListAsync(cancellationToken);
        return new IdeaListPage(items, total, page, pageSize);
    }

    private async Task<string> GenerateNextCodeAsync(CancellationToken cancellationToken)
    {
        var codes = await _db.Ideas.Select(i => i.Code).ToListAsync(cancellationToken);
        var maxNumber = 0;
        foreach (var code in codes)
        {
            var parts = code.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[1], out var number) && number > maxNumber)
            {
                maxNumber = number;
            }
        }
        return $"IDEA-{(maxNumber + 1):D4}";
    }
}
