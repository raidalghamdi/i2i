using InnovationToImpact.Domain.Audit;
using InnovationToImpact.Domain.Auth;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Ideas;
using InnovationToImpact.Domain.Notifications;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Linq;

namespace InnovationToImpact.Infrastructure.Ideas;

public class IdeaService : IIdeaService
{
    private readonly InnovationDbContext _db;
    private readonly IEvidenceFileStorage _storage;
    private readonly IAuditLogWriter _auditLogWriter;
    private readonly INotificationService _notificationService;
    private readonly IAdIdentityLookupService _adIdentityLookupService;

    public IdeaService(InnovationDbContext db, IEvidenceFileStorage storage, IAuditLogWriter auditLogWriter, INotificationService notificationService, IAdIdentityLookupService adIdentityLookupService)
    {
        _db = db;
        _storage = storage;
        _auditLogWriter = auditLogWriter;
        _notificationService = notificationService;
        _adIdentityLookupService = adIdentityLookupService;
    }

    private async Task<(IdeaCommandStatus? Status, IReadOnlyDictionary<string, AdIdentity> ResolvedMembers)> ValidateNewFieldsAsync(IdeaInput input, string? ownerSam, CancellationToken cancellationToken)
    {
        var activityExists = await _db.Activities.AnyAsync(a => a.Id == input.ActivityId, cancellationToken);
        if (!activityExists) return (IdeaCommandStatus.InvalidActivity, EmptyResolvedMembers);

        if (input.ChallengeId is Guid challengeId)
        {
            var challengeValid = await _db.Challenges.AnyAsync(c => c.Id == challengeId && c.StrategicThemeId == input.StrategicThemeId && c.IsActive, cancellationToken);
            if (!challengeValid) return (IdeaCommandStatus.InvalidChallenge, EmptyResolvedMembers);
        }
        else
        {
            var hasActiveChallenges = await _db.Challenges.AnyAsync(c => c.StrategicThemeId == input.StrategicThemeId && c.IsActive, cancellationToken);
            if (hasActiveChallenges) return (IdeaCommandStatus.InvalidChallenge, EmptyResolvedMembers);
        }

        if (input.ParticipationType != "individual" && input.ParticipationType != "team") return (IdeaCommandStatus.InvalidParticipation, EmptyResolvedMembers);

        var (teamStatus, resolvedMembers) = await ValidateAndResolveTeamMembersAsync(input.ParticipationType, input.TeamName, input.TeamMembers, ownerSam, cancellationToken);
        if (teamStatus is not null) return (teamStatus, EmptyResolvedMembers);

        if (!input.IpAcknowledged || !input.TermsAgreed) return (IdeaCommandStatus.ConsentRequired, EmptyResolvedMembers);

        return (null, resolvedMembers);
    }

    /// <summary>
    /// Shared team-roster gate used by create/update/resubmit. For team participation it requires a
    /// team name and 1–4 additional members (the owner is the first team member, so a team of size 1
    /// pairs with the owner for a 2-person team) each carrying a non-empty SamAccountName, resolves each
    /// SAM against AD (unknown SAM → <see cref="IdeaCommandStatus.TeamMemberNotFound"/>), and returns the
    /// resolved AD identities keyed by SAM so the persisted rows can store AD-authoritative name/email.
    /// The roster must not include the idea's own owner (<paramref name="ownerSam"/>, case-insensitive):
    /// the owner and team-member sets are kept disjoint so the read-only team-member gate can never hide
    /// an owner's own edit controls.
    /// </summary>
    private async Task<(IdeaCommandStatus? Status, IReadOnlyDictionary<string, AdIdentity> ResolvedMembers)> ValidateAndResolveTeamMembersAsync(
        string participationType, string? teamName, IReadOnlyList<TeamMemberInput> members, string? ownerSam, CancellationToken cancellationToken)
    {
        if (participationType != "team") return (null, EmptyResolvedMembers);

        if (string.IsNullOrWhiteSpace(teamName)) return (IdeaCommandStatus.InvalidParticipation, EmptyResolvedMembers);
        if (members.Count < 1 || members.Count > 4) return (IdeaCommandStatus.InvalidParticipation, EmptyResolvedMembers);

        var resolvedMembers = new Dictionary<string, AdIdentity>(StringComparer.OrdinalIgnoreCase);
        foreach (var member in members)
        {
            if (string.IsNullOrWhiteSpace(member.SamAccountName)) return (IdeaCommandStatus.InvalidParticipation, EmptyResolvedMembers);
            if (!string.IsNullOrEmpty(ownerSam) && string.Equals(member.SamAccountName, ownerSam, StringComparison.OrdinalIgnoreCase))
                return (IdeaCommandStatus.InvalidParticipation, EmptyResolvedMembers);
            var identity = await _adIdentityLookupService.ResolveAsync(member.SamAccountName, cancellationToken);
            if (identity is null) return (IdeaCommandStatus.TeamMemberNotFound, EmptyResolvedMembers);
            resolvedMembers[member.SamAccountName] = identity;
        }

        return (null, resolvedMembers);
    }

    private Task<string?> ResolveOwnerSamAsync(Guid submitterId, CancellationToken cancellationToken) =>
        _db.Users.Where(u => u.Id == submitterId).Select(u => u.SamAccountName).SingleOrDefaultAsync(cancellationToken);

    private static readonly IReadOnlyDictionary<string, AdIdentity> EmptyResolvedMembers = new Dictionary<string, AdIdentity>();

    private static void ReplaceTeamMembers(InnovationDbContext db, Guid ideaId, string participationType, IReadOnlyList<TeamMemberInput> members, IReadOnlyDictionary<string, AdIdentity> resolvedMembers)
    {
        var existing = db.IdeaTeamMembers.Where(m => m.IdeaId == ideaId);
        db.IdeaTeamMembers.RemoveRange(existing);
        if (participationType != "team") return;
        var index = 0;
        foreach (var member in members)
        {
            var identity = resolvedMembers[member.SamAccountName];
            db.IdeaTeamMembers.Add(new IdeaTeamMember { Id = Guid.NewGuid(), IdeaId = ideaId, SamAccountName = member.SamAccountName, Name = identity.DisplayName, Email = identity.Email, SortOrder = index });
            index++;
        }
    }

    public async Task<IdeaQueryResult> CreateAsync(Guid submitterId, IdeaInput input, CancellationToken cancellationToken = default)
    {
        var themeExists = await _db.StrategicThemes.AnyAsync(t => t.Id == input.StrategicThemeId, cancellationToken);
        if (!themeExists) return new IdeaQueryResult(IdeaCommandStatus.InvalidStrategicTheme);

        var ownerSam = await ResolveOwnerSamAsync(submitterId, cancellationToken);
        var (validationError, resolvedMembers) = await ValidateNewFieldsAsync(input, ownerSam, cancellationToken);
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
        ReplaceTeamMembers(_db, idea.Id, input.ParticipationType, input.TeamMembers, resolvedMembers);
        await _db.SaveChangesAsync(cancellationToken);

        return new IdeaQueryResult(IdeaCommandStatus.Success, idea);
    }

    /// <summary>Statuses whose owner may still edit the idea in place.</summary> // Change 20260726
    private static readonly string[] EditableStatuses = // Change 20260726
        { IdeaStatusCodes.Draft, IdeaStatusCodes.NeedsCompletion, IdeaStatusCodes.Returned }; // Change 20260726

    public async Task<IdeaQueryResult> UpdateAsync(Guid ideaId, Guid submitterId, IdeaInput input, CancellationToken cancellationToken = default)
    {
        var idea = await _db.Ideas.Include(i => i.IdeaStatus).SingleOrDefaultAsync(i => i.Id == ideaId, cancellationToken);
        if (idea is null) return new IdeaQueryResult(IdeaCommandStatus.NotFound);
        if (idea.SubmitterId != submitterId) return new IdeaQueryResult(IdeaCommandStatus.Forbidden);
        if (!EditableStatuses.Contains(idea.IdeaStatus.Code)) return new IdeaQueryResult(IdeaCommandStatus.InvalidState); // Change 20260726

        var themeExists = await _db.StrategicThemes.AnyAsync(t => t.Id == input.StrategicThemeId, cancellationToken);
        if (!themeExists) return new IdeaQueryResult(IdeaCommandStatus.InvalidStrategicTheme);

        var ownerSam = await ResolveOwnerSamAsync(idea.SubmitterId, cancellationToken);
        var (validationError, resolvedMembers) = await ValidateNewFieldsAsync(input, ownerSam, cancellationToken);
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
        ReplaceTeamMembers(_db, idea.Id, input.ParticipationType, input.TeamMembers, resolvedMembers);
        idea.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);

        await _auditLogWriter.AppendAsync("idea", idea.Id, "idea.updated", submitterId, // Change 20260726
            JsonSerializer.Serialize(new { status = idea.IdeaStatus.Code }), cancellationToken); // Change 20260726

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
        if (!allowed.Contains("problem_statement") && (input.ProblemStatementAr != idea.ProblemStatementAr || input.ProblemStatementEn != idea.ProblemStatementEn)) return new IdeaQueryResult(IdeaCommandStatus.SectionNotEditable);
        if (!allowed.Contains("proposed_solution") && (input.ProposedSolutionAr != idea.ProposedSolutionAr || input.ProposedSolutionEn != idea.ProposedSolutionEn)) return new IdeaQueryResult(IdeaCommandStatus.SectionNotEditable);
        if (!allowed.Contains("expected_benefits") && (input.ExpectedBenefitsAr != idea.ExpectedBenefitsAr || input.ExpectedBenefitsEn != idea.ExpectedBenefitsEn)) return new IdeaQueryResult(IdeaCommandStatus.SectionNotEditable);
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

        var ownerSam = await ResolveOwnerSamAsync(idea.SubmitterId, cancellationToken);
        var (teamStatus, resolvedMembers) = await ValidateAndResolveTeamMembersAsync(input.ParticipationType, input.TeamName, input.TeamMembers, ownerSam, cancellationToken);
        if (teamStatus is not null) return new IdeaQueryResult(teamStatus.Value);

        idea.TitleAr = input.TitleAr;
        idea.TitleEn = input.TitleEn;
        idea.ProblemStatementAr = input.ProblemStatementAr;
        idea.ProblemStatementEn = input.ProblemStatementEn;
        idea.ProposedSolutionAr = input.ProposedSolutionAr;
        idea.ProposedSolutionEn = input.ProposedSolutionEn;
        idea.ExpectedBenefitsAr = input.ExpectedBenefitsAr;
        idea.ExpectedBenefitsEn = input.ExpectedBenefitsEn;
        idea.ActivityId = input.ActivityId;
        idea.StrategicThemeId = input.StrategicThemeId;
        idea.ChallengeId = input.ChallengeId;
        idea.ParticipationType = input.ParticipationType;
        idea.TeamName = input.ParticipationType == "team" ? input.TeamName : null;

        if (allowed.Contains("team"))
        {
            ReplaceTeamMembers(_db, idea.Id, input.ParticipationType, input.TeamMembers, resolvedMembers);
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

    public async Task<IdeaQueryResult> WithdrawAsync(Guid ideaId, Guid submitterId, string? reason = null, CancellationToken cancellationToken = default) // Change 20260726
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
            JsonSerializer.Serialize(new { before = beforeStatus, after = IdeaStatusCodes.Withdrawn, reason }), cancellationToken); // Change 20260726

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

    public async Task<IReadOnlyList<MyIdeaItem>> GetMineDetailedAsync(Guid userId, string? statusGroup, string userEmail, string? callerSam = null, CancellationToken cancellationToken = default)
    {
        // Team membership is matched by SAM (authoritative) and, for backward compatibility with team
        // rows that predate SAM capture, also by email — mirrors the scoping in ListAsync so a caller's
        // "My ideas" list includes ideas they submitted as well as ideas they are a team member of.
        var callerSamLower = callerSam?.ToLowerInvariant();
        var query = _db.Ideas.Include(i => i.IdeaStatus).Where(i => i.SubmitterId == userId ||
            (!string.IsNullOrEmpty(userEmail) && i.TeamMembers.Any(tm => tm.Email == userEmail)) ||
            (!string.IsNullOrEmpty(callerSamLower) && i.TeamMembers.Any(tm => tm.SamAccountName != null && tm.SamAccountName.ToLower() == callerSamLower)));
        if (!string.IsNullOrWhiteSpace(statusGroup) && StatusGroups.TryGetValue(statusGroup, out var codes))
            query = query.Where(i => codes.Contains(i.IdeaStatus.Code));

        var ideas = await query.OrderByDescending(i => i.UpdatedAt)
            .Select(i => new { i.Id, i.Code, i.TitleAr, i.TitleEn, Status = i.IdeaStatus.Code, i.CurrentStage, i.CreatedAt, i.UpdatedAt, i.SubmitterId, i.StrategicThemeId, ThemeNameAr = i.StrategicTheme.NameAr, ThemeNameEn = i.StrategicTheme.NameEn }) // Change 20260726
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

        return ideas.Select(i => new MyIdeaItem(i.Id, i.Code, i.TitleAr, i.TitleEn, i.Status, i.CurrentStage, i.CreatedAt, i.UpdatedAt, feedback[i.Id], i.SubmitterId == userId, i.StrategicThemeId, i.ThemeNameAr, i.ThemeNameEn)).ToList(); // Change 20260726
    }

    public async Task<IdeaQueryResult> GetByIdAsync(Guid ideaId, Guid submitterId, bool isElevatedReviewer = false, string? callerSam = null, CancellationToken cancellationToken = default)
    {
        var idea = await _db.Ideas.Include(i => i.IdeaStatus).Include(i => i.TeamMembers).Include(i => i.Submitter).SingleOrDefaultAsync(i => i.Id == ideaId, cancellationToken);
        if (idea is null) return new IdeaQueryResult(IdeaCommandStatus.NotFound);
        if (idea.SubmitterId != submitterId && !isElevatedReviewer && !IsTeamMemberBySam(idea, callerSam)) return new IdeaQueryResult(IdeaCommandStatus.Forbidden);
        return new IdeaQueryResult(IdeaCommandStatus.Success, idea);
    }

    private static bool IsTeamMemberBySam(Idea idea, string? callerSam) =>
        !string.IsNullOrEmpty(callerSam) &&
        idea.TeamMembers.Any(m => m.SamAccountName != null && string.Equals(m.SamAccountName, callerSam, StringComparison.OrdinalIgnoreCase));

    // A returned idea accepts new attachments only when the "attachments" section was left editable
    // (a null/empty EditableSections means every section is editable, matching ResubmitAsync).
    private static bool ReturnedAllowsAttachments(Idea idea) =>
        idea.IdeaStatus.Code == IdeaStatusCodes.Returned &&
        (string.IsNullOrWhiteSpace(idea.EditableSections) ||
         idea.EditableSections.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Contains("attachments"));

    public async Task<IdeaAttachmentResult> AddAttachmentAsync(Guid ideaId, Guid submitterId, string fileName, string contentType, byte[] content, CancellationToken cancellationToken = default)
    {
        var idea = await _db.Ideas.Include(i => i.IdeaStatus).SingleOrDefaultAsync(i => i.Id == ideaId, cancellationToken);
        if (idea is null) return new IdeaAttachmentResult(IdeaCommandStatus.NotFound);
        if (idea.SubmitterId != submitterId) return new IdeaAttachmentResult(IdeaCommandStatus.Forbidden);
        if (idea.IdeaStatus.Code != IdeaStatusCodes.Draft
            && idea.IdeaStatus.Code != IdeaStatusCodes.PassAwaitingAttachments
            && !ReturnedAllowsAttachments(idea))
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

    public async Task<IdeaAttachmentsResult> GetAttachmentsAsync(Guid ideaId, Guid submitterId, bool isElevatedReviewer = false, string? callerSam = null, CancellationToken cancellationToken = default)
    {
        var idea = await _db.Ideas.Include(i => i.TeamMembers).SingleOrDefaultAsync(i => i.Id == ideaId, cancellationToken);
        if (idea is null) return new IdeaAttachmentsResult(IdeaCommandStatus.NotFound, Array.Empty<EvidenceAttachment>());
        if (idea.SubmitterId != submitterId && !isElevatedReviewer && !IsTeamMemberBySam(idea, callerSam)) return new IdeaAttachmentsResult(IdeaCommandStatus.Forbidden, Array.Empty<EvidenceAttachment>());

        var attachments = await _db.EvidenceAttachments
            .Where(a => a.EntityType == EvidenceEntityTypes.Idea && a.EntityId == ideaId && a.DeletedAt == null)
            .OrderBy(a => a.UploadedAt)
            .ToListAsync(cancellationToken);

        return new IdeaAttachmentsResult(IdeaCommandStatus.Success, attachments);
    }

    public async Task<IdeaAttachmentsResult> DeleteAttachmentAsync(Guid ideaId, Guid attachmentId, Guid submitterId, CancellationToken cancellationToken = default) // Change 20260726
    { // Change 20260726
        var idea = await _db.Ideas.Include(i => i.IdeaStatus).SingleOrDefaultAsync(i => i.Id == ideaId, cancellationToken); // Change 20260726
        if (idea is null) return new IdeaAttachmentsResult(IdeaCommandStatus.NotFound, Array.Empty<EvidenceAttachment>()); // Change 20260726
        if (idea.SubmitterId != submitterId) return new IdeaAttachmentsResult(IdeaCommandStatus.Forbidden, Array.Empty<EvidenceAttachment>()); // Change 20260726
        if (idea.IdeaStatus.Code != IdeaStatusCodes.Draft // Change 20260726
            && idea.IdeaStatus.Code != IdeaStatusCodes.PassAwaitingAttachments // Change 20260726
            && !ReturnedAllowsAttachments(idea)) // Change 20260726
        { // Change 20260726
            return new IdeaAttachmentsResult(IdeaCommandStatus.InvalidState, Array.Empty<EvidenceAttachment>()); // Change 20260726
        } // Change 20260726

        var attachment = await _db.EvidenceAttachments // Change 20260726
            .SingleOrDefaultAsync(a => a.Id == attachmentId && a.EntityType == EvidenceEntityTypes.Idea && a.EntityId == ideaId && a.DeletedAt == null, cancellationToken); // Change 20260726
        if (attachment is null) return new IdeaAttachmentsResult(IdeaCommandStatus.NotFound, Array.Empty<EvidenceAttachment>()); // Change 20260726

        attachment.DeletedAt = DateTime.UtcNow; // Change 20260726
        await _db.SaveChangesAsync(cancellationToken); // Change 20260726

        await _auditLogWriter.AppendAsync("idea", ideaId, "idea.attachment_deleted", submitterId, // Change 20260726
            JsonSerializer.Serialize(new { attachmentId, fileName = attachment.FileName }), cancellationToken); // Change 20260726

        var remaining = await _db.EvidenceAttachments // Change 20260726
            .Where(a => a.EntityType == EvidenceEntityTypes.Idea && a.EntityId == ideaId && a.DeletedAt == null) // Change 20260726
            .OrderBy(a => a.UploadedAt) // Change 20260726
            .ToListAsync(cancellationToken); // Change 20260726

        return new IdeaAttachmentsResult(IdeaCommandStatus.Success, remaining); // Change 20260726
    } // Change 20260726

    public async Task<IdeaAttachmentFileResult> GetAttachmentFileAsync(Guid ideaId, Guid attachmentId, Guid userId, bool isElevatedReviewer, string? callerSam, CancellationToken ct = default)
    {
        var idea = await _db.Ideas.Include(i => i.TeamMembers).SingleOrDefaultAsync(i => i.Id == ideaId, ct);
        if (idea is null) return new IdeaAttachmentFileResult(IdeaCommandStatus.NotFound);
        if (idea.SubmitterId != userId && !isElevatedReviewer && !IsTeamMemberBySam(idea, callerSam)) return new IdeaAttachmentFileResult(IdeaCommandStatus.Forbidden);

        var attachment = await _db.EvidenceAttachments
            .SingleOrDefaultAsync(a => a.Id == attachmentId && a.EntityType == EvidenceEntityTypes.Idea && a.EntityId == ideaId && a.DeletedAt == null, ct);
        if (attachment is null) return new IdeaAttachmentFileResult(IdeaCommandStatus.NotFound);

        var content = await _storage.ReadAsync(attachment.BlobPath, ct);
        return new IdeaAttachmentFileResult(IdeaCommandStatus.Success, content, attachment.ContentType, attachment.FileName);
    }

    private static readonly string[] JudgeFinalistStatuses =
    {
        IdeaStatusCodes.Committee, IdeaStatusCodes.Approved,
        IdeaStatusCodes.InPilot, IdeaStatusCodes.InMeasurement, IdeaStatusCodes.InScaling,
    };

    public async Task<IdeaListPage> ListAsync(IdeaListFilter filter, Guid userId, string userEmail, IReadOnlyCollection<string> roles, string? callerSam = null, CancellationToken cancellationToken = default)
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
            // Team membership is matched by SAM (authoritative, added in Task B2) and, for backward
            // compatibility with team rows that predate SAM capture, also by email (legacy match kept
            // so callers such as SearchService that don't thread callerSam through still work, and so
            // pre-existing team rows without a SamAccountName remain visible to their members).
            // SAM match is case-insensitive (consistent with GetByIdAsync/GetAttachmentsAsync); lower-cased
            // on both sides so the comparison is EF-translatable on SQLite/dev.
            var callerSamLower = callerSam?.ToLowerInvariant();
            query = query.Where(i => i.SubmitterId == userId ||
                (!string.IsNullOrEmpty(userEmail) && i.TeamMembers.Any(tm => tm.Email == userEmail)) ||
                (!string.IsNullOrEmpty(callerSamLower) && i.TeamMembers.Any(tm => tm.SamAccountName != null && tm.SamAccountName.ToLower() == callerSamLower)));
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
