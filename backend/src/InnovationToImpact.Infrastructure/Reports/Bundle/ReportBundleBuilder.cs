using System.Globalization;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Ideas;
using InnovationToImpact.Domain.Reports.Bundle;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Reports.Bundle;

/// <summary>
/// Builds <see cref="ReportBundle"/>s for all 12 legacy report types. Seven are
/// ideas-based (executive, detailed, ideas, media, themes, innovators, trends);
/// the remaining five (cx, operational, audit, evaluators, committee) are direct
/// reads over AuditLogs, SlaTrackings, Evaluations, CommitteeDecisions and
/// SupportMessages respectively.
/// </summary>
public class ReportBundleBuilder : IReportBundleBuilder
{
    private readonly InnovationDbContext _db;

    // Status buckets per the Phase 5.5 design spec ("Status buckets" section),
    // built from IdeaStatusCodes constants only — never string literals.
    private static readonly HashSet<string> ApprovedOrBeyond = new()
    {
        IdeaStatusCodes.Approved,
        IdeaStatusCodes.InPilot,
        IdeaStatusCodes.InMeasurement,
        IdeaStatusCodes.InScaling,
    };

    private static readonly HashSet<string> ImplementedBucket = new()
    {
        IdeaStatusCodes.InPilot,
        IdeaStatusCodes.InMeasurement,
        IdeaStatusCodes.InScaling,
    };

    private static readonly HashSet<string> InProgressBucket = new()
    {
        IdeaStatusCodes.Submitted,
        IdeaStatusCodes.Evaluation,
        IdeaStatusCodes.PassAwaitingAttachments,
        IdeaStatusCodes.Committee,
        IdeaStatusCodes.PendingFinalRanking,
    };

    private static readonly HashSet<string> RejectedBucket = new() { IdeaStatusCodes.Rejected };

    private static readonly HashSet<string> ReturnedBucket = new() { IdeaStatusCodes.Returned };

    public ReportBundleBuilder(InnovationDbContext db)
    {
        _db = db;
    }

    public async Task<ReportBundle> BuildAsync(
        string type,
        DateTime? from,
        DateTime? to,
        Guid? themeId,
        string generatedBy,
        CancellationToken cancellationToken = default)
    {
        return type switch
        {
            ReportTypeCodes.Executive => await BuildExecutiveAsync(from, to, themeId, generatedBy, cancellationToken),
            ReportTypeCodes.Detailed => await BuildDetailedAsync(ReportTypeCodes.Detailed, from, to, themeId, generatedBy, cancellationToken),
            ReportTypeCodes.Ideas => await BuildDetailedAsync(ReportTypeCodes.Ideas, from, to, themeId, generatedBy, cancellationToken),
            ReportTypeCodes.Media => await BuildMediaAsync(from, to, themeId, generatedBy, cancellationToken),
            ReportTypeCodes.Themes => await BuildThemesAsync(from, to, themeId, generatedBy, cancellationToken),
            ReportTypeCodes.Innovators => await BuildInnovatorsAsync(from, to, themeId, generatedBy, cancellationToken),
            ReportTypeCodes.Trends => await BuildTrendsAsync(from, to, themeId, generatedBy, cancellationToken),
            ReportTypeCodes.Cx => await BuildCxAsync(from, to, generatedBy, cancellationToken),
            ReportTypeCodes.Operational => await BuildOperationalAsync(from, to, generatedBy, cancellationToken),
            ReportTypeCodes.Audit => await BuildAuditAsync(from, to, generatedBy, cancellationToken),
            ReportTypeCodes.Evaluators => await BuildEvaluatorsAsync(from, to, generatedBy, cancellationToken),
            ReportTypeCodes.Committee => await BuildCommitteeAsync(from, to, generatedBy, cancellationToken),
            _ => throw new NotSupportedException(type),
        };
    }

    private async Task<List<Idea>> LoadIdeasAsync(DateTime? from, DateTime? to, Guid? themeId, CancellationToken ct)
    {
        var query = _db.Ideas
            .Include(i => i.IdeaStatus)
            .Include(i => i.StrategicTheme)
            .Include(i => i.Submitter)
            .AsQueryable();

        if (from.HasValue)
        {
            query = query.Where(i => i.CreatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(i => i.CreatedAt <= to.Value);
        }

        if (themeId.HasValue)
        {
            query = query.Where(i => i.StrategicThemeId == themeId.Value);
        }

        return await query.ToListAsync(ct);
    }

    private static string Pct(int part, int total) =>
        total == 0 ? "0%" : Math.Round(part * 100.0 / total, MidpointRounding.AwayFromZero).ToString(CultureInfo.InvariantCulture) + "%";

    private static string DateStr(DateTime value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string MonthStr(DateTime value) => value.ToString("yyyy-MM", CultureInfo.InvariantCulture);

    private async Task<ReportBundle> BuildExecutiveAsync(DateTime? from, DateTime? to, Guid? themeId, string generatedBy, CancellationToken ct)
    {
        var ideas = await LoadIdeasAsync(from, to, themeId, ct);
        var total = ideas.Count;
        var approved = ideas.Count(i => ApprovedOrBeyond.Contains(i.IdeaStatus.Code));
        var rejected = ideas.Count(i => RejectedBucket.Contains(i.IdeaStatus.Code));
        var inProgress = ideas.Count(i => InProgressBucket.Contains(i.IdeaStatus.Code));
        var implemented = ideas.Count(i => ImplementedBucket.Contains(i.IdeaStatus.Code));

        var kpis = new List<ReportKpi>
        {
            new("Total", "الإجمالي", total.ToString(CultureInfo.InvariantCulture)),
            new("Approved", "المعتمدة", approved.ToString(CultureInfo.InvariantCulture)),
            new("Rejected", "المرفوضة", rejected.ToString(CultureInfo.InvariantCulture)),
            new("In Progress", "قيد التنفيذ", inProgress.ToString(CultureInfo.InvariantCulture)),
            new("Implemented", "منفَّذة", implemented.ToString(CultureInfo.InvariantCulture)),
            new("Approval Rate", "معدل الاعتماد", Pct(approved, total)),
        };

        var columns = new List<ReportColumn>
        {
            new("theme", "Theme", "المسار الاستراتيجي"),
            new("count", "Count", "العدد"),
            new("share", "Share %", "النسبة المئوية"),
        };

        var rows = ideas
            .GroupBy(i => new { i.StrategicThemeId, i.StrategicTheme.NameEn })
            .OrderBy(g => g.Key.NameEn, StringComparer.Ordinal)
            .Select(g => new Dictionary<string, string>
            {
                ["theme"] = g.Key.NameEn,
                ["count"] = g.Count().ToString(CultureInfo.InvariantCulture),
                ["share"] = Pct(g.Count(), total),
            })
            .ToList();

        var section = new ReportSection(
            "Distribution by Strategic Theme",
            "التوزيع حسب المسار الاستراتيجي",
            columns,
            rows);

        return new ReportBundle(ReportTypeCodes.Executive, DateTime.UtcNow, generatedBy, from, to, kpis, new[] { section });
    }

    private async Task<ReportBundle> BuildDetailedAsync(string type, DateTime? from, DateTime? to, Guid? themeId, string generatedBy, CancellationToken ct)
    {
        var ideas = await LoadIdeasAsync(from, to, themeId, ct);

        var kpis = new List<ReportKpi>
        {
            new("Ideas Included", "عدد الأفكار المشمولة", ideas.Count.ToString(CultureInfo.InvariantCulture)),
        };

        var columns = new List<ReportColumn>
        {
            new("code", "Code", "الرمز"),
            new("title", "Title", "العنوان"),
            new("status", "Status", "الحالة"),
            new("theme", "Theme", "المسار"),
            new("submitter", "Submitter", "مقدم الفكرة"),
            new("stage", "Stage", "المرحلة"),
            new("created_at", "Created At", "تاريخ الإنشاء"),
        };

        var rows = ideas
            .OrderBy(i => i.Code, StringComparer.Ordinal)
            .Select(i => new Dictionary<string, string>
            {
                ["code"] = i.Code,
                ["title"] = i.TitleEn,
                ["status"] = i.IdeaStatus.Code,
                ["theme"] = i.StrategicTheme.NameEn,
                ["submitter"] = i.Submitter.FullNameEn,
                ["stage"] = i.CurrentStage.ToString(CultureInfo.InvariantCulture),
                ["created_at"] = DateStr(i.CreatedAt),
            })
            .ToList();

        var section = new ReportSection(
            "Detailed Ideas Register",
            "سجل الأفكار التفصيلي",
            columns,
            rows);

        return new ReportBundle(type, DateTime.UtcNow, generatedBy, from, to, kpis, new[] { section });
    }

    private async Task<ReportBundle> BuildMediaAsync(DateTime? from, DateTime? to, Guid? themeId, string generatedBy, CancellationToken ct)
    {
        var ideas = await LoadIdeasAsync(from, to, themeId, ct);
        // Approximation (confirmed): legacy filters by confidentiality='public', which the
        // new schema has no field for. We publish all approved-or-beyond ("highlight") ideas.
        var highlights = ideas.Where(i => ApprovedOrBeyond.Contains(i.IdeaStatus.Code)).ToList();

        var kpis = new List<ReportKpi>
        {
            new("Highlight Stories", "القصص البارزة", highlights.Count.ToString(CultureInfo.InvariantCulture)),
            new("Publishable", "القابلة للنشر", highlights.Count.ToString(CultureInfo.InvariantCulture)),
        };

        var columns = new List<ReportColumn>
        {
            new("title", "Title", "العنوان"),
            new("expected_benefit", "Expected Benefit", "المنفعة المتوقعة"),
            new("stage", "Stage", "المرحلة"),
            new("theme", "Theme", "المسار"),
        };

        var rows = highlights
            .OrderBy(i => i.Code, StringComparer.Ordinal)
            .Select(i => new Dictionary<string, string>
            {
                ["title"] = i.TitleEn,
                ["expected_benefit"] = i.ExpectedBenefitsEn,
                ["stage"] = i.IdeaStatus.Code,
                ["theme"] = i.StrategicTheme.NameEn,
            })
            .ToList();

        var section = new ReportSection(
            "Publication-Ready Stories",
            "قصص جاهزة للنشر",
            columns,
            rows);

        return new ReportBundle(ReportTypeCodes.Media, DateTime.UtcNow, generatedBy, from, to, kpis, new[] { section });
    }

    private async Task<ReportBundle> BuildThemesAsync(DateTime? from, DateTime? to, Guid? themeId, string generatedBy, CancellationToken ct)
    {
        var ideas = await LoadIdeasAsync(from, to, themeId, ct);

        var columns = new List<ReportColumn>
        {
            new("theme", "Theme", "المسار"),
            new("count", "Count", "العدد"),
            new("approved", "Approved", "المعتمدة"),
            new("rejected", "Rejected", "المرفوضة"),
            new("approval_rate", "Approval Rate", "معدل الاعتماد"),
        };

        var rows = ideas
            .GroupBy(i => new { i.StrategicThemeId, i.StrategicTheme.NameEn })
            .OrderBy(g => g.Key.NameEn, StringComparer.Ordinal)
            .Select(g =>
            {
                var count = g.Count();
                var approved = g.Count(i => ApprovedOrBeyond.Contains(i.IdeaStatus.Code));
                var rejected = g.Count(i => RejectedBucket.Contains(i.IdeaStatus.Code));
                return new Dictionary<string, string>
                {
                    ["theme"] = g.Key.NameEn,
                    ["count"] = count.ToString(CultureInfo.InvariantCulture),
                    ["approved"] = approved.ToString(CultureInfo.InvariantCulture),
                    ["rejected"] = rejected.ToString(CultureInfo.InvariantCulture),
                    ["approval_rate"] = Pct(approved, count),
                };
            })
            .ToList();

        var kpis = new List<ReportKpi>
        {
            new("Themes", "عدد المسارات", rows.Count.ToString(CultureInfo.InvariantCulture)),
            new("Total Ideas", "إجمالي الأفكار", ideas.Count.ToString(CultureInfo.InvariantCulture)),
        };

        var section = new ReportSection("Themes Performance", "أداء المسارات الاستراتيجية", columns, rows);

        return new ReportBundle(ReportTypeCodes.Themes, DateTime.UtcNow, generatedBy, from, to, kpis, new[] { section });
    }

    private async Task<ReportBundle> BuildInnovatorsAsync(DateTime? from, DateTime? to, Guid? themeId, string generatedBy, CancellationToken ct)
    {
        var ideas = await LoadIdeasAsync(from, to, themeId, ct);

        var columns = new List<ReportColumn>
        {
            new("innovator", "Innovator", "المُبتكِر"),
            new("ideas", "Ideas", "الأفكار"),
            new("approved", "Approved", "المعتمدة"),
        };

        var rows = ideas
            .GroupBy(i => new { i.SubmitterId, i.Submitter.FullNameEn })
            .OrderBy(g => g.Key.FullNameEn, StringComparer.Ordinal)
            .Select(g => new Dictionary<string, string>
            {
                ["innovator"] = g.Key.FullNameEn,
                ["ideas"] = g.Count().ToString(CultureInfo.InvariantCulture),
                ["approved"] = g.Count(i => ApprovedOrBeyond.Contains(i.IdeaStatus.Code)).ToString(CultureInfo.InvariantCulture),
            })
            .ToList();

        var kpis = new List<ReportKpi>
        {
            new("Active Innovators", "المُبتكِرون النشطون", rows.Count.ToString(CultureInfo.InvariantCulture)),
        };

        var section = new ReportSection("Innovators Roster", "سجل المُبتكِرين", columns, rows);

        return new ReportBundle(ReportTypeCodes.Innovators, DateTime.UtcNow, generatedBy, from, to, kpis, new[] { section });
    }

    private async Task<ReportBundle> BuildTrendsAsync(DateTime? from, DateTime? to, Guid? themeId, string generatedBy, CancellationToken ct)
    {
        var ideas = await LoadIdeasAsync(from, to, themeId, ct);

        var columns = new List<ReportColumn>
        {
            new("month", "Month", "الشهر"),
            new("submitted", "Submitted", "المُقدَّمة"),
            new("approved", "Approved", "المعتمدة"),
            new("approval_rate", "Approval Rate", "معدل الاعتماد"),
        };

        var rows = ideas
            .GroupBy(i => MonthStr(i.CreatedAt))
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g =>
            {
                var count = g.Count();
                var approved = g.Count(i => ApprovedOrBeyond.Contains(i.IdeaStatus.Code));
                return new Dictionary<string, string>
                {
                    ["month"] = g.Key,
                    ["submitted"] = count.ToString(CultureInfo.InvariantCulture),
                    ["approved"] = approved.ToString(CultureInfo.InvariantCulture),
                    ["approval_rate"] = Pct(approved, count),
                };
            })
            .ToList();

        var kpis = new List<ReportKpi>
        {
            new("Months Covered", "عدد الأشهر", rows.Count.ToString(CultureInfo.InvariantCulture)),
            new("Total Ideas", "إجمالي الأفكار", ideas.Count.ToString(CultureInfo.InvariantCulture)),
        };

        var section = new ReportSection("Monthly Trend", "الاتجاه الشهري", columns, rows);

        return new ReportBundle(ReportTypeCodes.Trends, DateTime.UtcNow, generatedBy, from, to, kpis, new[] { section });
    }

    // ─────────────────────────────────────────────────────────────────────
    // Direct-read builders (not sourced from the Ideas graph).
    // ─────────────────────────────────────────────────────────────────────

    private async Task<ReportBundle> BuildAuditAsync(DateTime? from, DateTime? to, string generatedBy, CancellationToken ct)
    {
        // AuditLog's timestamp column is OccurredAt (not CreatedAt).
        var query = _db.AuditLogs.AsQueryable();

        if (from.HasValue)
        {
            query = query.Where(a => a.OccurredAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(a => a.OccurredAt <= to.Value);
        }

        var logs = await query
            .OrderByDescending(a => a.OccurredAt)
            .Take(1000)
            .ToListAsync(ct);

        var kpis = new List<ReportKpi>
        {
            new("Total Records", "إجمالي السجلات", logs.Count.ToString(CultureInfo.InvariantCulture)),
        };

        var columns = new List<ReportColumn>
        {
            new("date", "Date", "التاريخ"),
            new("actor", "Actor", "الفاعل"),
            new("entity_type", "Entity Type", "نوع الكيان"),
            new("entity_id", "Entity ID", "معرّف الكيان"),
            new("action", "Action", "الفعل"),
        };

        var rows = logs
            .Select(a => new Dictionary<string, string>
            {
                ["date"] = DateStr(a.OccurredAt),
                ["actor"] = a.ActorId?.ToString() ?? "",
                ["entity_type"] = a.EntityType,
                ["entity_id"] = a.EntityId.ToString(),
                ["action"] = a.Action,
            })
            .ToList();

        var section = new ReportSection("Audit Trail", "سجل المراجعة", columns, rows);

        return new ReportBundle(ReportTypeCodes.Audit, DateTime.UtcNow, generatedBy, from, to, kpis, new[] { section });
    }

    private async Task<ReportBundle> BuildOperationalAsync(DateTime? from, DateTime? to, string generatedBy, CancellationToken ct)
    {
        // SlaTracking has no EntityType/SlaType/Status columns of its own — entity/sla_type
        // derive from the related SlaPolicy, and status derives from BreachedAt/ResolvedAt.
        var slaQuery = _db.SlaTrackings.Include(s => s.SlaPolicy).AsQueryable();

        if (from.HasValue)
        {
            slaQuery = slaQuery.Where(s => s.TargetAt >= from.Value);
        }

        if (to.HasValue)
        {
            slaQuery = slaQuery.Where(s => s.TargetAt <= to.Value);
        }

        var slaRows = await slaQuery.ToListAsync(ct);

        // Windowed to match the report's date range, same as the ideas-based builders
        // (independent from/to bounds on Idea.CreatedAt; themeId does not apply here).
        var ideasQuery = _db.Ideas.Include(i => i.IdeaStatus).AsQueryable();

        if (from.HasValue)
        {
            ideasQuery = ideasQuery.Where(i => i.CreatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            ideasQuery = ideasQuery.Where(i => i.CreatedAt <= to.Value);
        }

        var ideas = await ideasQuery.ToListAsync(ct);
        var ideasInQueue = ideas.Count(i => InProgressBucket.Contains(i.IdeaStatus.Code));
        var slaBreaches = slaRows.Count(s => s.BreachedAt != null);

        var kpis = new List<ReportKpi>
        {
            new("Ideas In Queue", "أفكار في الطوابير", ideasInQueue.ToString(CultureInfo.InvariantCulture)),
            new("SLA Breaches", "خروقات اتفاقيات الخدمة", slaBreaches.ToString(CultureInfo.InvariantCulture)),
            new("SLA Records", "إجمالي سجلات الخدمة", slaRows.Count.ToString(CultureInfo.InvariantCulture)),
        };

        var columns = new List<ReportColumn>
        {
            new("entity", "Entity", "الكيان"),
            new("sla_type", "SLA Type", "نوع الخدمة"),
            new("due_at", "Due", "المستحقة في"),
            new("status", "Status", "الحالة"),
            new("breached_at", "Breached At", "تاريخ الخرق"),
        };

        var rows = slaRows
            .Select(s => new Dictionary<string, string>
            {
                ["entity"] = $"{s.SlaPolicy.EntityType} {s.EntityId}".Trim(),
                ["sla_type"] = $"{s.SlaPolicy.FromState}→{s.SlaPolicy.ToState}",
                ["due_at"] = DateStr(s.TargetAt),
                ["status"] = s.BreachedAt != null ? "breached" : s.ResolvedAt != null ? "resolved" : "open",
                ["breached_at"] = s.BreachedAt.HasValue ? DateStr(s.BreachedAt.Value) : "",
            })
            .ToList();

        var section = new ReportSection("SLA Health", "حالة اتفاقيات الخدمة", columns, rows);

        return new ReportBundle(ReportTypeCodes.Operational, DateTime.UtcNow, generatedBy, from, to, kpis, new[] { section });
    }

    private async Task<ReportBundle> BuildEvaluatorsAsync(DateTime? from, DateTime? to, string generatedBy, CancellationToken ct)
    {
        // Evaluation has no status enum and no CreatedAt column — "submitted" derives from
        // SubmittedAt != null. The range filter applies each bound independently, like every
        // other builder; when any bound is set, evaluations with no SubmittedAt are excluded
        // (they have no date to place in the window). avg_score below is still computed over
        // ALL rows in the resulting filtered group, not just the submitted ones.
        var query = _db.Evaluations.Include(e => e.Evaluator).AsQueryable();

        if (from.HasValue)
        {
            query = query.Where(e => e.SubmittedAt != null && e.SubmittedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(e => e.SubmittedAt != null && e.SubmittedAt <= to.Value);
        }

        var evaluations = await query.ToListAsync(ct);

        var rows = evaluations
            .GroupBy(e => new { e.EvaluatorId, e.Evaluator.FullNameEn })
            .Select(g => new
            {
                g.Key.FullNameEn,
                Count = g.Count(),
                Submitted = g.Count(e => e.SubmittedAt != null),
                Avg = Math.Round(g.Average(e => (double)e.TotalScore), 1),
            })
            .OrderByDescending(x => x.Count)
            .Select(x => new Dictionary<string, string>
            {
                ["evaluator"] = x.FullNameEn,
                ["evaluations"] = x.Count.ToString(CultureInfo.InvariantCulture),
                ["submitted"] = x.Submitted.ToString(CultureInfo.InvariantCulture),
                ["avg_score"] = x.Avg.ToString(CultureInfo.InvariantCulture),
            })
            .ToList();

        var kpis = new List<ReportKpi>
        {
            new("Total Evaluations", "إجمالي التقييمات", evaluations.Count.ToString(CultureInfo.InvariantCulture)),
            new("Active Evaluators", "المُقيّمون النشطون", rows.Count.ToString(CultureInfo.InvariantCulture)),
        };

        var columns = new List<ReportColumn>
        {
            new("evaluator", "Evaluator", "المُقيّم"),
            new("evaluations", "Evaluations", "عدد التقييمات"),
            new("submitted", "Submitted", "مُقدّمة"),
            new("avg_score", "Avg Score", "متوسط الدرجة"),
        };

        var section = new ReportSection("Evaluator Performance", "أداء المُقيّمين", columns, rows);

        return new ReportBundle(ReportTypeCodes.Evaluators, DateTime.UtcNow, generatedBy, from, to, kpis, new[] { section });
    }

    private async Task<ReportBundle> BuildCommitteeAsync(DateTime? from, DateTime? to, string generatedBy, CancellationToken ct)
    {
        var query = _db.CommitteeDecisions.Include(c => c.CommitteeDecisionType).AsQueryable();

        if (from.HasValue)
        {
            query = query.Where(c => c.DecidedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(c => c.DecidedAt <= to.Value);
        }

        var decisions = await query.OrderByDescending(c => c.DecidedAt).ToListAsync(ct);

        var kpis = new List<ReportKpi>
        {
            new("Total Decisions", "إجمالي القرارات", decisions.Count.ToString(CultureInfo.InvariantCulture)),
        };

        var columns = new List<ReportColumn>
        {
            new("date", "Date", "التاريخ"),
            new("committee", "Committee", "اللجنة"),
            new("idea", "Idea", "الفكرة"),
            new("decision", "Decision", "القرار"),
            new("quorum", "Quorum", "اكتمل النصاب"),
        };

        var rows = decisions
            .Select(c => new Dictionary<string, string>
            {
                ["date"] = c.DecidedAt.HasValue ? DateStr(c.DecidedAt.Value) : "",
                ["committee"] = c.CommitteeName,
                ["idea"] = c.IdeaId.ToString(),
                ["decision"] = c.CommitteeDecisionType.Code,
                ["quorum"] = c.QuorumMet ? "Yes" : "No",
            })
            .ToList();

        var section = new ReportSection("Committee Decisions", "قرارات اللجنة", columns, rows);

        return new ReportBundle(ReportTypeCodes.Committee, DateTime.UtcNow, generatedBy, from, to, kpis, new[] { section });
    }

    private async Task<ReportBundle> BuildCxAsync(DateTime? from, DateTime? to, string generatedBy, CancellationToken ct)
    {
        // Confirmed approximation: legacy read an idea_feedback table that does not exist in
        // the new schema; the section is sourced from SupportMessages instead.
        // Windowed to match the report's date range (independent from/to bounds on
        // Idea.CreatedAt, same as the ideas-based builders; themeId does not apply here).
        var returnedQuery = _db.Ideas.Where(i => i.IdeaStatus.Code == IdeaStatusCodes.Returned);

        if (from.HasValue)
        {
            returnedQuery = returnedQuery.Where(i => i.CreatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            returnedQuery = returnedQuery.Where(i => i.CreatedAt <= to.Value);
        }

        var returnedCount = await returnedQuery.CountAsync(ct);

        var query = _db.SupportMessages.AsQueryable();

        if (from.HasValue)
        {
            query = query.Where(m => m.CreatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(m => m.CreatedAt <= to.Value);
        }

        var messages = await query.ToListAsync(ct);

        var kpis = new List<ReportKpi>
        {
            new("Ideas Returned to Innovator", "أفكار مُعادة للمبتكر", returnedCount.ToString(CultureInfo.InvariantCulture)),
            new("Feedback Entries", "ملاحظات مُستلمة", messages.Count.ToString(CultureInfo.InvariantCulture)),
        };

        var columns = new List<ReportColumn>
        {
            new("name", "Sender", "المرسِل"),
            new("subject", "Subject", "الموضوع"),
            new("body", "Message", "المحتوى"),
            new("date", "Date", "التاريخ"),
        };

        var rows = messages
            .Select(m => new Dictionary<string, string>
            {
                ["name"] = m.Name,
                ["subject"] = m.Subject,
                ["body"] = m.Body,
                ["date"] = DateStr(m.CreatedAt),
            })
            .ToList();

        var section = new ReportSection("Feedback and Interactions", "الملاحظات والتفاعلات", columns, rows);

        return new ReportBundle(ReportTypeCodes.Cx, DateTime.UtcNow, generatedBy, from, to, kpis, new[] { section });
    }
}
