using System.Security.Claims;
using System.Text.Json;
using InnovationToImpact.Api.Auth;
using InnovationToImpact.Api.Notifications;
using InnovationToImpact.Domain.Analytics;
using InnovationToImpact.Domain.Approvals;
using InnovationToImpact.Domain.Assignments;
using InnovationToImpact.Domain.Audit;
using InnovationToImpact.Domain.Auth;
using InnovationToImpact.Domain.Backup;
using InnovationToImpact.Domain.Briefing;
using InnovationToImpact.Domain.Cms;
using InnovationToImpact.Domain.Committee;
using InnovationToImpact.Domain.CommitteeReferral;
using InnovationToImpact.Domain.Content;
using InnovationToImpact.Domain.Dashboards;
using InnovationToImpact.Domain.Email;
using InnovationToImpact.Domain.EmailTemplates;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Escalations;
using InnovationToImpact.Domain.EvaluationReview;
using InnovationToImpact.Domain.SubmitterReview;
using InnovationToImpact.Domain.Evaluations;
using InnovationToImpact.Domain.FinalRanking;
using InnovationToImpact.Domain.Home;
using InnovationToImpact.Domain.Ideas;
using InnovationToImpact.Domain.Invitations;
using InnovationToImpact.Domain.Notifications;
using InnovationToImpact.Domain.Phases;
using InnovationToImpact.Domain.PostProgram;
using InnovationToImpact.Domain.Reports;
using InnovationToImpact.Domain.Reports.Bundle;
using InnovationToImpact.Domain.Roster;
using InnovationToImpact.Domain.Screening;
using InnovationToImpact.Domain.Search;
using InnovationToImpact.Domain.Settings;
using InnovationToImpact.Domain.Sla;
using InnovationToImpact.Domain.StrategicThemes;
using InnovationToImpact.Domain.TrackAssignments;
using InnovationToImpact.Domain.UserManagement;
using InnovationToImpact.Infrastructure.Analytics;
using InnovationToImpact.Infrastructure.Approvals;
using InnovationToImpact.Infrastructure.Assignments;
using InnovationToImpact.Infrastructure.Audit;
using InnovationToImpact.Infrastructure.Auth;
using InnovationToImpact.Infrastructure.Backup;
using InnovationToImpact.Infrastructure.Briefing;
using InnovationToImpact.Infrastructure.Cms;
using InnovationToImpact.Infrastructure.Committee;
using InnovationToImpact.Infrastructure.CommitteeReferral;
using InnovationToImpact.Infrastructure.Content;
using InnovationToImpact.Infrastructure.Dashboards;
using InnovationToImpact.Infrastructure.Data;
using InnovationToImpact.Infrastructure.Email;
using InnovationToImpact.Infrastructure.EmailTemplates;
using InnovationToImpact.Infrastructure.Escalations;
using InnovationToImpact.Infrastructure.EvaluationReview;
using InnovationToImpact.Infrastructure.SubmitterReview;
using InnovationToImpact.Infrastructure.Evaluations;
using InnovationToImpact.Infrastructure.FinalRanking;
using InnovationToImpact.Infrastructure.Home;
using InnovationToImpact.Infrastructure.Ideas;
using InnovationToImpact.Infrastructure.Invitations;
using InnovationToImpact.Infrastructure.Notifications;
using InnovationToImpact.Infrastructure.Phases;
using InnovationToImpact.Infrastructure.PostProgram;
using InnovationToImpact.Infrastructure.Reports;
using InnovationToImpact.Infrastructure.Reports.Bundle;
using InnovationToImpact.Infrastructure.Roster;
using InnovationToImpact.Infrastructure.Screening;
using InnovationToImpact.Infrastructure.Search;
using InnovationToImpact.Infrastructure.Settings;
using InnovationToImpact.Infrastructure.Sla;
using InnovationToImpact.Infrastructure.Scheduling;
using InnovationToImpact.Infrastructure.Storage;
using InnovationToImpact.Infrastructure.StrategicThemes;
using InnovationToImpact.Infrastructure.TrackAssignments;
using InnovationToImpact.Infrastructure.UserManagement;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<InnovationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("InnovationDb")));

builder.Services.AddMemoryCache();
builder.Services.Configure<ActiveDirectoryOptions>(builder.Configuration.GetSection("ActiveDirectory"));
builder.Services.AddScoped<IClaimsTransformation, IdentityClaimsTransformation>();
builder.Services.AddScoped<IAuditLogWriter, AuditLogWriter>();
builder.Services.AddScoped<IAuditChainVerifier, AuditChainVerifier>();
builder.Services.AddScoped<IAuditBrowseService, AuditBrowseService>();
builder.Services.Configure<EmailOutboxOptions>(builder.Configuration.GetSection("EmailOutbox"));
builder.Services.AddSingleton<IEmailSender, LoggingEmailSender>();
builder.Services.AddScoped<IEmailOutboxProcessor, EmailOutboxProcessor>();
builder.Services.AddScoped<ISlaScanner, SlaScanner>();
builder.Services.AddScoped<ISlaClockService, SlaClockService>();
builder.Services.AddScoped<IEscalationService, EscalationService>();
builder.Services.AddScoped<IApprovalService, ApprovalService>();
builder.Services.AddScoped<ISlaScanOrchestrator, SlaScanOrchestrator>();
// Change 20260726
builder.Services.AddScoped<ISlaPolicyService, SlaPolicyService>();
builder.Services.AddHostedService<ReminderSchedulerHostedService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.Configure<InvitationReminderOptions>(builder.Configuration.GetSection("InvitationReminders"));
builder.Services.AddScoped<IInvitationReminderProcessor, InvitationReminderProcessor>();
builder.Services.AddScoped<IInvitationReminderSettingsService, InvitationReminderSettingsService>();
builder.Services.AddScoped<IRosterService, RosterService>();
builder.Services.AddScoped<IRoleInvitationSettingsService, RoleInvitationSettingsService>();
builder.Services.AddScoped<IRoleInvitationReminderProcessor, RoleInvitationReminderProcessor>();
builder.Services.Configure<WeeklyBriefingOptions>(builder.Configuration.GetSection("WeeklyBriefing"));
builder.Services.AddScoped<IWeeklyBriefingProcessor, WeeklyBriefingProcessor>();
builder.Services.AddSignalR();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IIdeaStatusNotifier, IdeaStatusNotifier>(); // Change 20260726
builder.Services.AddSingleton<INotificationPublisher, SignalRNotificationPublisher>();
builder.Services.Configure<ReportStorageOptions>(builder.Configuration.GetSection("ReportStorage"));
builder.Services.AddSingleton<IReportFileStorage>(sp =>
    new LocalDiskFileStorage(sp.GetRequiredService<IOptions<ReportStorageOptions>>().Value.RootPath));
builder.Services.AddScoped<IAuditLogReportGenerator, AuditLogReportGenerator>();
builder.Services.AddScoped<IIdeasReportGenerator, IdeasReportGenerator>();
builder.Services.AddScoped<IEvaluationsReportGenerator, EvaluationsReportGenerator>();
builder.Services.AddScoped<IEvaluatorProductivityService, EvaluatorProductivityService>(); // Change 20260726
builder.Services.AddScoped<IEscalationsReportGenerator, EscalationsReportGenerator>();
builder.Services.AddScoped<IAnalyticsReportGenerator, AnalyticsReportGenerator>();
builder.Services.AddScoped<IReportBundleBuilder, ReportBundleBuilder>();
builder.Services.AddScoped<IReportBundleXlsxRenderer, ReportBundleXlsxRenderer>();
builder.Services.AddScoped<IReportBundlePdfRenderer, ReportBundlePdfRenderer>();
builder.Services.AddScoped<IReportBundlePptxRenderer, ReportBundlePptxRenderer>();
builder.Services.AddScoped<IReportGenerationService, ReportGenerationService>();
builder.Services.Configure<EvidenceStorageOptions>(builder.Configuration.GetSection("EvidenceStorage"));
builder.Services.AddSingleton<IEvidenceFileStorage>(sp =>
    new LocalDiskFileStorage(sp.GetRequiredService<IOptions<EvidenceStorageOptions>>().Value.RootPath));
builder.Services.Configure<TemplateStorageOptions>(builder.Configuration.GetSection("TemplateStorage"));
builder.Services.AddSingleton<IIdeaTemplateFileStorage>(sp =>
    new LocalDiskFileStorage(sp.GetRequiredService<IOptions<TemplateStorageOptions>>().Value.RootPath));
builder.Services.AddScoped<IIdeaTemplateService, IdeaTemplateService>();
builder.Services.Configure<HomeMediaStorageOptions>(builder.Configuration.GetSection("HomeMediaStorage"));
builder.Services.AddSingleton<IHomeMediaFileStorage>(sp =>
    new LocalDiskFileStorage(sp.GetRequiredService<IOptions<HomeMediaStorageOptions>>().Value.RootPath));
builder.Services.AddScoped<IHomeMediaService, HomeMediaService>();
builder.Services.AddScoped<IIdeaService, IdeaService>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<IChallengeService, ChallengeService>();
builder.Services.AddScoped<IEvaluationService, EvaluationService>();
builder.Services.AddScoped<IEvaluationSettingsService, EvaluationSettingsService>();
builder.Services.AddScoped<IEvaluationCriteriaService, EvaluationCriteriaService>(); // Change 20260726
builder.Services.AddScoped<ICommitteeService, CommitteeService>();
builder.Services.AddScoped<ICommitteeReferralService, CommitteeReferralService>();
builder.Services.AddScoped<ICommitteeCriteriaService, CommitteeCriteriaService>();
builder.Services.AddScoped<IBackupExportService, BackupExportService>();
builder.Services.AddScoped<IPlatformSettingsService, PlatformSettingsService>();
builder.Services.AddScoped<IRolesCatalogService, RolesCatalogService>();
builder.Services.AddScoped<IScreeningService, ScreeningService>();
builder.Services.AddScoped<IEvaluationReviewService, EvaluationReviewService>();
builder.Services.AddScoped<ISubmitterReviewService, SubmitterReviewService>();
builder.Services.AddScoped<IPostProgramService, PostProgramService>();
builder.Services.AddScoped<ITrackAssignmentService, TrackAssignmentService>();
builder.Services.AddScoped<IPhaseScheduleService, PhaseScheduleService>();
builder.Services.AddScoped<IStrategicThemeService, StrategicThemeService>();
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
builder.Services.Configure<EmailTemplateAttachmentStorageOptions>(builder.Configuration.GetSection("EmailTemplateAttachmentStorage"));
builder.Services.AddSingleton<IEmailTemplateAttachmentFileStorage>(sp =>
    new LocalDiskFileStorage(sp.GetRequiredService<IOptions<EmailTemplateAttachmentStorageOptions>>().Value.RootPath));
builder.Services.AddScoped<IEmailTemplateAttachmentService, EmailTemplateAttachmentService>();
builder.Services.AddScoped<IAssignmentService, AssignmentService>();
builder.Services.AddScoped<IFinalRankingService, FinalRankingService>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<ICmsService, CmsService>();
builder.Services.AddScoped<IPublicContentService, PublicContentService>();
builder.Services.AddScoped<IPublicDataService, PublicDataService>();
builder.Services.AddScoped<ISupportInboxService, SupportInboxService>();
builder.Services.AddScoped<IHomePageService, HomePageService>();

if (builder.Environment.IsProduction())
{
    builder.Services.AddSingleton<IAdIdentityLookupService, LdapIdentityLookupService>();
    builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
        .AddNegotiate();
}
else
{
    // NOTE: registered via an explicit factory rather than AddSingleton<IAdIdentityLookupService, FakeAdIdentityLookupService>()
    // directly. FakeAdIdentityLookupService's constructor takes an optional IEnumerable<AdIdentity>? seedIdentities = null.
    // The built-in DI container always resolves IEnumerable<T> requests (as an empty collection when nothing is registered)
    // rather than treating the parameter as unresolvable, so implicit constructor injection silently passes an EMPTY
    // collection instead of falling back to the C# default `null` -- which means DefaultIdentities() never runs and the
    // singleton ends up with zero seeded identities. Since DevAuthenticationHandler auto-authenticates every
    // unauthenticated request via DevAuth:SamAccountName, and IClaimsTransformation runs on every successful
    // authentication regardless of the endpoint's own [Authorize] requirement, this broke identity resolution (and thus
    // every request) in Development mode. Constructing it directly with `new` preserves the real default-parameter
    // semantics.
    builder.Services.AddSingleton<IAdIdentityLookupService>(_ => new FakeAdIdentityLookupService());
    builder.Services.Configure<DevAuthOptions>(builder.Configuration.GetSection("DevAuth"));
    // DevAuth trusts an unauthenticated X-Dev-User header, so it must never be reachable from the // Change 20260726
    // public internet (this app also runs as Staging on a public host). The default scheme is a // Change 20260726
    // selector that only routes to DevAuth in Development or for private-IP callers; every other // Change 20260726
    // caller falls through to Negotiate and gets a real AD challenge. // Change 20260726
    builder.Services.AddAuthentication(DevAuthenticationDefaults.SelectorScheme) // Change 20260726
        .AddPolicyScheme(DevAuthenticationDefaults.SelectorScheme, DevAuthenticationDefaults.SelectorScheme, options => // Change 20260726
        { // Change 20260726
            options.ForwardDefaultSelector = context => // Change 20260726
            { // Change 20260726
                var environment = context.RequestServices.GetRequiredService<IWebHostEnvironment>(); // Change 20260726
                if (DevAuthAccessPolicy.IsAllowed(context, environment)) // Change 20260726
                { // Change 20260726
                    return DevAuthenticationDefaults.AuthenticationScheme; // Change 20260726
                } // Change 20260726

                if (context.Request.Headers.ContainsKey(DevAuthAccessPolicy.DevUserHeaderName)) // Change 20260726
                { // Change 20260726
                    context.RequestServices.GetRequiredService<ILogger<Program>>().LogWarning( // Change 20260726
                        "Rejected {Header} impersonation attempt from non-private address {ClientAddress}.", // Change 20260726
                        DevAuthAccessPolicy.DevUserHeaderName, // Change 20260726
                        DevAuthAccessPolicy.ResolveClientAddress(context)?.ToString() ?? "unknown"); // Change 20260726
                } // Change 20260726

                return NegotiateDefaults.AuthenticationScheme; // Change 20260726
            }; // Change 20260726
        }) // Change 20260726
        .AddScheme<DevAuthenticationOptions, DevAuthenticationHandler>(
            DevAuthenticationDefaults.AuthenticationScheme, _ => { }) // Change 20260726
        .AddNegotiate(); // Change 20260726
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(RoleCodes.Admin));
    options.AddPolicy("SupervisorOrCommittee", policy => policy.RequireRole(RoleCodes.Supervisor, RoleCodes.Judge, RoleCodes.Admin));
    options.AddPolicy("EvaluatorAndAbove", policy => policy.RequireRole(RoleCodes.Evaluator, RoleCodes.Judge, RoleCodes.Supervisor, RoleCodes.Admin));
    options.AddPolicy("AnyAssignedRole", policy => policy.RequireRole(RoleCodes.Submitter, RoleCodes.Evaluator, RoleCodes.Judge, RoleCodes.Supervisor, RoleCodes.Admin));
    options.AddPolicy("SupervisorOrAdmin", policy => policy.RequireRole(RoleCodes.Supervisor, RoleCodes.Admin));
    options.AddPolicy("IdeaAuthor", policy => policy.RequireRole(RoleCodes.Submitter, RoleCodes.Admin));
});

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var app = builder.Build();

// Surfaced at boot (visible in the hosting platform's logs) because DevAuth is an impersonation // Change 20260726
// backdoor: it must never read "yes" without the private-IP restriction outside Development. // Change 20260726
var devAuthState = app.Environment.IsProduction() // Change 20260726
    ? "no (Production — Negotiate only)" // Change 20260726
    : app.Environment.IsDevelopment() // Change 20260726
        ? "yes (Development — all callers)" // Change 20260726
        : "yes (private-IP callers only; others get Negotiate)"; // Change 20260726
app.Logger.LogInformation("DevAuth enabled: {DevAuthState}", devAuthState); // Change 20260726

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (IdentityResolutionUnavailableException ex)
    {
        context.RequestServices.GetRequiredService<ILogger<Program>>()
            .LogWarning(ex, "Identity resolution unavailable for {SamAccountName}", ex.SamAccountName);
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
    }
});

app.UseAuthentication();
app.UseAuthorization();

// Seed the initial "current" idea-description template, once, from a backend-embedded copy of the
// .docx that used to be a static frontend asset. Safe to call on every startup: it's a no-op once a
// row exists, and it's a no-op (rather than a crash) if the seed asset or the IdeaTemplates table
// isn't present yet (e.g. migrations haven't run). See IdeaTemplateSeeder for details.
// Gated to the real SQL Server provider (never SQLite) so it never fires under the in-memory/SQLite
// WebApplicationFactory test fixtures used throughout Api.Tests -- those don't override
// TemplateStorageOptions, so an unguarded seed would write into the default relative
// "template-storage" directory on every test run and would also break tests that assert
// "no template uploaded yet" (e.g. IdeaTemplateEndpointTests).
using (var templateSeedScope = app.Services.CreateScope())
{
    try
    {
        var seedDb = templateSeedScope.ServiceProvider.GetRequiredService<InnovationDbContext>();
        if (seedDb.Database.IsSqlServer())
        {
            var seedStorage = templateSeedScope.ServiceProvider.GetRequiredService<IIdeaTemplateFileStorage>();
            var seedAssetPath = Path.Combine(AppContext.BaseDirectory, "SeedAssets", "idea-description-template.docx");
            // Canonical "system" user seeded by the SeedStrategicThemesAndSystemUser migration.
            var systemUserId = new Guid("00000000-0000-0000-0026-000000000001");
            await IdeaTemplateSeeder.SeedIfMissingAsync(seedDb, seedStorage, seedAssetPath, systemUserId);
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Skipped seeding the initial idea-description template.");
    }
}

// Seed the initial 12 homepage sections with today's landing-page content, once, if the table is
// empty. Same SqlServer-only guard as the idea-template seeder above -- never fires under the
// SQLite WebApplicationFactory test fixtures (HomePageEndpointTests seeds directly instead).
using (var homePageSeedScope = app.Services.CreateScope())
{
    try
    {
        var homeDb = homePageSeedScope.ServiceProvider.GetRequiredService<InnovationDbContext>();
        if (homeDb.Database.IsSqlServer())
        {
            await HomePageSeeder.SeedIfMissingAsync(homeDb);
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Skipped seeding the initial homepage sections.");
    }
}

// DEV/TEST ONLY: provision 5 users per role (40) plus grant devuser every role, so the system can be
// exercised across all roles. Development environment only + SqlServer-only (never SQLite test fixtures); Change 20260726
// Staging is included because the cloud test deployment runs as Staging and needs the same roster. // Change 20260726
if (app.Environment.IsDevelopment() || app.Environment.IsStaging()) // Change 20260726
{
    using var devUsersScope = app.Services.CreateScope();
    try
    {
        var devDb = devUsersScope.ServiceProvider.GetRequiredService<InnovationDbContext>();
        if (devDb.Database.IsSqlServer())
        {
            await InnovationToImpact.Infrastructure.Auth.DevUsersSeeder.SeedAsync(devDb);
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Skipped seeding dev test users.");
    }
}

app.MapGet("/api/health/db", async (InnovationDbContext db) =>
{
    var canConnect = await db.Database.CanConnectAsync();
    return canConnect
        ? Results.Ok(new { status = "ok" })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
});

app.MapGet("/api/public/cms/content/{slug}", async (string slug, IPublicContentService service) =>
{
    var content = await service.GetPublishedBySlugAsync(slug);
    if (content is null) return Results.NotFound();
    return Results.Ok(new { slug = content.Slug, titleAr = content.TitleAr, titleEn = content.TitleEn, bodyAr = content.BodyAr, bodyEn = content.BodyEn });
});

app.MapGet("/api/public/tracks", async (IPublicDataService service) =>
    Results.Ok((await service.ListTracksAsync()).Select(t => new { id = t.Id, nameAr = t.NameAr, nameEn = t.NameEn, descriptionAr = t.DescriptionAr, descriptionEn = t.DescriptionEn, priority = t.Priority })));

app.MapGet("/api/public/tracks/{id:guid}", async (Guid id, IPublicDataService service) =>
{
    var d = await service.GetTrackAsync(id);
    if (d is null) return Results.NotFound();
    return Results.Ok(new
    {
        track = new { id = d.Track.Id, nameAr = d.Track.NameAr, nameEn = d.Track.NameEn, descriptionAr = d.Track.DescriptionAr, descriptionEn = d.Track.DescriptionEn },
        challenges = d.Challenges.Select(c => new { id = c.Id, textAr = c.TextAr, textEn = c.TextEn }),
        ideas = d.Ideas.Select(i => new { id = i.Id, code = i.Code, titleAr = i.TitleAr, titleEn = i.TitleEn, status = i.Status }),
    });
});

// Re-parses the stored ContentJson text into a nested JSON element for the response. Clone() detaches
// the element from the JsonDocument so it survives disposal -- without this the JsonDocument would leak.
static JsonElement ParseHomeContent(string contentJson)
{
    using var doc = JsonDocument.Parse(contentJson);
    return doc.RootElement.Clone();
}

// True when contentJson is well-formed JSON. Admin writes go straight to the DB text column and are
// re-parsed on every GET, so a malformed row would 500 the anonymous public homepage for everyone --
// reject it at the write boundary instead. Mirrors the PATCH /api/admin/settings/{key} validation.
static bool IsValidHomeContentJson(string? contentJson)
{
    if (string.IsNullOrEmpty(contentJson)) return false;
    try
    {
        using var _ = JsonDocument.Parse(contentJson);
        return true;
    }
    catch (JsonException)
    {
        return false;
    }
}

static object ToHomeSectionPublicDto(HomePageSection s) => new
{
    idx = s.Idx,
    type = s.Type,
    isVisible = s.IsVisible,
    contentJson = ParseHomeContent(s.ContentJson),
};

static object ToHomeSectionAdminDto(HomePageSection s) => new
{
    id = s.Id,
    idx = s.Idx,
    type = s.Type,
    isVisible = s.IsVisible,
    contentJson = ParseHomeContent(s.ContentJson),
    updatedAt = s.UpdatedAt,
    updatedById = s.UpdatedById,
};

app.MapGet("/api/public/home", async (IHomePageService service) =>
    Results.Ok((await service.ListPublicAsync()).Select(ToHomeSectionPublicDto)));

app.MapGet("/api/admin/home/sections", async (IHomePageService service) =>
    Results.Ok((await service.ListAllAsync()).Select(ToHomeSectionAdminDto)))
    .RequireAuthorization("AdminOnly");

app.MapPut("/api/admin/home/sections", async (HomePageSectionsReplaceRequest body, ClaimsPrincipal user, IHomePageService service) =>
{
    if (body.Sections.Any(s => !IsValidHomeContentJson(s.ContentJson)))
        return Results.BadRequest(new { error = "Every section's contentJson must be valid JSON." });

    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    await service.ReplaceAllAsync(body.Sections, actorId);
    return Results.Ok((await service.ListAllAsync()).Select(ToHomeSectionAdminDto));
}).RequireAuthorization("AdminOnly");

app.MapPost("/api/admin/home/sections", async (HomePageSectionInput input, ClaimsPrincipal user, IHomePageService service) =>
{
    if (!IsValidHomeContentJson(input.ContentJson))
        return Results.BadRequest(new { error = "contentJson must be valid JSON." });

    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var section = await service.AddAsync(input, actorId);
    return Results.Ok(ToHomeSectionAdminDto(section));
}).RequireAuthorization("AdminOnly");

app.MapDelete("/api/admin/home/sections/{id:guid}", async (Guid id, IHomePageService service) =>
{
    await service.DeleteAsync(id);
    return Results.NoContent();
}).RequireAuthorization("AdminOnly");

app.MapGet("/api/public/activities", async (IPublicDataService service) =>
    Results.Ok((await service.ListActivitiesAsync()).Select(a => new { id = a.Id, nameAr = a.NameAr, nameEn = a.NameEn, type = a.Type, status = a.Status, startDate = a.StartDate, endDate = a.EndDate, ideaCount = a.IdeaCount })));

app.MapGet("/api/public/activities/{id:guid}", async (Guid id, IPublicDataService service) =>
{
    var d = await service.GetActivityAsync(id);
    if (d is null) return Results.NotFound();
    return Results.Ok(new
    {
        activity = new { id = d.Activity.Id, nameAr = d.Activity.NameAr, nameEn = d.Activity.NameEn, type = d.Activity.Type, status = d.Activity.Status, startDate = d.Activity.StartDate, endDate = d.Activity.EndDate, ideaCount = d.Activity.IdeaCount },
        approvedCount = d.ApprovedCount,
        pilotingCount = d.PilotingCount,
        ideas = d.Ideas.Select(i => new { id = i.Id, code = i.Code, titleAr = i.TitleAr, titleEn = i.TitleEn, status = i.Status }),
    });
});

app.MapGet("/api/public/search", async (string? q, IPublicDataService service) =>
{
    var results = await service.SearchAsync(q ?? string.Empty);
    return Results.Ok(new
    {
        ideas = results.Ideas.Select(i => new { id = i.Id, code = i.Code, titleAr = i.TitleAr, titleEn = i.TitleEn, status = i.Status }),
        tracks = results.Tracks.Select(t => new { id = t.Id, nameAr = t.NameAr, nameEn = t.NameEn, descriptionAr = t.DescriptionAr, descriptionEn = t.DescriptionEn, priority = t.Priority }),
    });
});

app.MapPost("/api/public/support", async (SupportSubmitInput input, InnovationDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(input.Message) || string.IsNullOrWhiteSpace(input.Email))
        return Results.BadRequest(new { error = "Email and message are required." });
    if ((input.Name?.Length ?? 0) > 200 || input.Email.Length > 200 || (input.Subject?.Length ?? 0) > 200 || input.Message.Length > 4000)
        return Results.BadRequest(new { error = "One or more fields exceed the maximum length." });
    db.Set<SupportMessage>().Add(new SupportMessage
    {
        Id = Guid.NewGuid(),
        Name = input.Name ?? string.Empty,
        Email = input.Email,
        Subject = input.Subject ?? string.Empty,
        Body = input.Message,
    });
    await db.SaveChangesAsync();
    return Results.Ok(new { ok = true });
});

app.MapGet("/api/identity/me", (ClaimsPrincipal user) => Results.Ok(new
{
    samAccountName = user.Identity?.Name,
    email = user.FindFirstValue(ClaimTypes.Email),
    department = user.FindFirstValue("department"),
    roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray()
})).RequireAuthorization();

app.MapGet("/api/me", async (ClaimsPrincipal user, InnovationDbContext db) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var dbUser = await db.Users
        .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
        .SingleAsync(u => u.Id == userId);

    return Results.Ok(new
    {
        id = dbUser.Id,
        samAccountName = dbUser.SamAccountName,
        email = dbUser.Email,
        fullNameAr = dbUser.FullNameAr,
        fullNameEn = dbUser.FullNameEn,
        department = dbUser.Department,
        title = dbUser.Title,
        points = dbUser.Points,
        level = dbUser.Level,
        roles = dbUser.UserRoles.Where(ur => ur.Role.IsActive).Select(ur => ur.Role.Code).ToArray(),
    });
}).RequireAuthorization();

app.MapGet("/api/me/badges", async (ClaimsPrincipal user, InnovationDbContext db) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var earnedByBadgeId = await db.UserBadges
        .Where(ub => ub.UserId == userId)
        .ToDictionaryAsync(ub => ub.BadgeId, ub => ub.AwardedAt);

    var badges = await db.Badges.Where(b => b.IsActive).ToListAsync();

    return Results.Ok(new
    {
        badges = badges.Select(b => new
        {
            code = b.Code,
            nameAr = b.NameAr,
            nameEn = b.NameEn,
            descriptionAr = b.DescriptionAr,
            descriptionEn = b.DescriptionEn,
            iconUrl = b.IconUrl,
            earnedAt = earnedByBadgeId.TryGetValue(b.Id, out var awardedAt) ? awardedAt : (DateTime?)null,
        }),
    });
}).RequireAuthorization();

app.MapGet("/api/notifications", async (ClaimsPrincipal user, InnovationDbContext db) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var notifications = await db.Notifications
        .Where(n => n.UserId == userId)
        .OrderByDescending(n => n.CreatedAt)
        .ToListAsync();

    return Results.Ok(notifications.Select(n => new
    {
        id = n.Id,
        notificationType = n.NotificationType,
        titleAr = n.TitleAr,
        titleEn = n.TitleEn,
        bodyAr = n.BodyAr,
        bodyEn = n.BodyEn,
        link = n.Link,
        readAt = n.ReadAt,
        createdAt = n.CreatedAt,
    }));
}).RequireAuthorization();

app.MapPost("/api/notifications/{id:guid}/read", async (Guid id, ClaimsPrincipal user, InnovationDbContext db) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var notification = await db.Notifications.SingleOrDefaultAsync(n => n.Id == id && n.UserId == userId);
    if (notification is null) return Results.NotFound();

    notification.ReadAt ??= DateTime.UtcNow;
    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        id = notification.Id,
        notificationType = notification.NotificationType,
        titleAr = notification.TitleAr,
        titleEn = notification.TitleEn,
        bodyAr = notification.BodyAr,
        bodyEn = notification.BodyEn,
        link = notification.Link,
        readAt = notification.ReadAt,
        createdAt = notification.CreatedAt,
    });
}).RequireAuthorization();

app.MapPost("/api/notifications/read-all", async (ClaimsPrincipal user, InnovationDbContext db) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var unread = await db.Notifications.Where(n => n.UserId == userId && n.ReadAt == null).ToListAsync();
    var now = DateTime.UtcNow;
    foreach (var notification in unread) notification.ReadAt = now;
    await db.SaveChangesAsync();

    return Results.Ok(new { markedCount = unread.Count });
}).RequireAuthorization();

// Change 20260726
app.MapGet("/api/notifications/categories", () => Results.Ok(NotificationCategories.All.Select(c => new
{
    key = c.Key,
    labelAr = c.LabelAr,
    labelEn = c.LabelEn,
}))).RequireAuthorization();

// Change 20260726
app.MapGet("/api/notifications/preferences", async (ClaimsPrincipal user, InnovationDbContext db) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var stored = await db.NotificationPreferences
        .Where(p => p.UserId == userId)
        .ToDictionaryAsync(p => p.CategoryKey, p => p.Muted);

    return Results.Ok(NotificationCategories.All.Select(c => new
    {
        categoryKey = c.Key,
        labelAr = c.LabelAr,
        labelEn = c.LabelEn,
        muted = stored.TryGetValue(c.Key, out var muted) && muted,
    }));
}).RequireAuthorization();

// Change 20260726
app.MapPut("/api/notifications/preferences", async (
    NotificationPreferencesUpdateRequest request,
    ClaimsPrincipal user,
    InnovationDbContext db) =>
{
    var items = request.Preferences ?? new List<NotificationPreferenceItem>();

    var unknown = items.Select(i => i.CategoryKey ?? string.Empty)
        .Where(key => !NotificationCategories.IsKnown(key))
        .Distinct()
        .ToList();
    if (unknown.Count > 0)
    {
        return Results.BadRequest(new { error = "unknown_category", categories = unknown });
    }

    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var existing = await db.NotificationPreferences
        .Where(p => p.UserId == userId)
        .ToDictionaryAsync(p => p.CategoryKey);
    var now = DateTime.UtcNow;

    // Last item wins so a duplicated category cannot violate the unique index.
    foreach (var item in items.GroupBy(i => i.CategoryKey!).Select(g => g.Last()))
    {
        if (existing.TryGetValue(item.CategoryKey!, out var preference))
        {
            if (preference.Muted == item.Muted) continue;
            preference.Muted = item.Muted;
            preference.UpdatedAt = now;
        }
        else
        {
            db.NotificationPreferences.Add(new NotificationPreference
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CategoryKey = item.CategoryKey!,
                Muted = item.Muted,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }
    }

    await db.SaveChangesAsync();

    var stored = await db.NotificationPreferences
        .Where(p => p.UserId == userId)
        .ToDictionaryAsync(p => p.CategoryKey, p => p.Muted);

    return Results.Ok(NotificationCategories.All.Select(c => new
    {
        categoryKey = c.Key,
        labelAr = c.LabelAr,
        labelEn = c.LabelEn,
        muted = stored.TryGetValue(c.Key, out var muted) && muted,
    }));
}).RequireAuthorization();

app.MapGet("/api/admin/ping", (ClaimsPrincipal user) => Results.Ok(new { samAccountName = user.Identity?.Name }))
    .RequireAuthorization("AdminOnly");

app.MapGet("/api/supervisor/ping", (ClaimsPrincipal user) => Results.Ok(new { samAccountName = user.Identity?.Name }))
    .RequireAuthorization("SupervisorOrCommittee");

app.MapGet("/api/evaluator/ping", (ClaimsPrincipal user) => Results.Ok(new { samAccountName = user.Identity?.Name }))
    .RequireAuthorization("EvaluatorAndAbove");

app.MapGet("/api/submitter/ping", (ClaimsPrincipal user) => Results.Ok(new { samAccountName = user.Identity?.Name }))
    .RequireAuthorization("AnyAssignedRole");

app.MapGet("/api/admin/audit/verify", async (IAuditChainVerifier verifier) =>
{
    var result = await verifier.VerifyAsync();
    return Results.Ok(new { isValid = result.IsValid, brokenAtChainSeq = result.BrokenAtChainSeq });
}).RequireAuthorization("AdminOnly");

app.MapGet("/api/admin/audit", async (string? entityType, string? action, Guid? actorId, DateTime? from, DateTime? to, int? page, int? pageSize, IAuditBrowseService service) =>
{
    var filter = new AuditBrowseFilter(entityType, action, actorId, from, to, page ?? 1, pageSize ?? 25);
    var result = await service.BrowseAsync(filter);
    return Results.Ok(new
    {
        items = result.Items.Select(r => new
        {
            id = r.Id,
            chainSeq = r.ChainSeq,
            occurredAt = r.OccurredAt,
            actorName = r.ActorName,
            entityType = r.EntityType,
            entityId = r.EntityId,
            action = r.Action,
            verified = r.Verified,
        }),
        total = result.Total,
        page = result.Page,
        pageSize = result.PageSize,
    });
}).RequireAuthorization("AdminOnly");

app.MapGet("/api/admin/email-log", async (int? page, int? pageSize, string? status, InnovationDbContext db) =>
{
    var query = db.EmailLogs.Include(e => e.EmailLogStatus).AsQueryable();
    if (!string.IsNullOrWhiteSpace(status))
        query = query.Where(e => e.EmailLogStatus.Code == status);

    var total = await query.CountAsync();
    var clampedPage = page is null or < 1 ? 1 : page.Value;
    var clampedPageSize = pageSize is null or < 1 or > 100 ? 25 : pageSize.Value;

    var items = await query
        .OrderByDescending(e => e.SentAt)
        .Skip((clampedPage - 1) * clampedPageSize)
        .Take(clampedPageSize)
        .Select(e => new
        {
            id = e.Id,
            provider = e.Provider,
            statusCode = e.EmailLogStatus.Code,
            statusNameAr = e.EmailLogStatus.NameAr,
            statusNameEn = e.EmailLogStatus.NameEn,
            providerMessageId = e.ProviderMessageId,
            redirectApplied = e.RedirectApplied,
            toEmail = e.ToEmail,
            sentAt = e.SentAt,
        })
        .ToListAsync();

    return Results.Ok(new { items, total, page = clampedPage, pageSize = clampedPageSize });
}).RequireAuthorization("AdminOnly");

app.MapGet("/api/admin/compliance", async (InnovationDbContext db) =>
{
    var controls = await db.ComplianceControls
        .Include(c => c.StandardBody)
        .Include(c => c.ComplianceControlStatus)
        .OrderBy(c => c.StandardBody.SortOrder)
        .ThenBy(c => c.ControlCode)
        .ToListAsync();

    var items = controls.Select(c => new
    {
        id = c.Id,
        controlCode = c.ControlCode,
        standardBodyCode = c.StandardBody.Code,
        standardBodyNameAr = c.StandardBody.NameAr,
        standardBodyNameEn = c.StandardBody.NameEn,
        titleAr = c.TitleAr,
        titleEn = c.TitleEn,
        descriptionAr = c.DescriptionAr,
        descriptionEn = c.DescriptionEn,
        statusCode = c.ComplianceControlStatus.Code,
        statusNameAr = c.ComplianceControlStatus.NameAr,
        statusNameEn = c.ComplianceControlStatus.NameEn,
        mappedFeaturePaths = string.IsNullOrWhiteSpace(c.MappedFeaturePathsJson)
            ? Array.Empty<string>()
            : JsonSerializer.Deserialize<string[]>(c.MappedFeaturePathsJson) ?? Array.Empty<string>(),
    });

    return Results.Ok(items);
}).RequireAuthorization("AdminOnly");

app.MapGet("/api/admin/support", async (int? page, int? pageSize, bool? handled, ISupportInboxService service) =>
{
    var filter = new SupportInboxFilter(handled, page ?? 1, pageSize ?? 25);
    var result = await service.ListAsync(filter);
    return Results.Ok(new
    {
        items = result.Items.Select(m => new
        {
            id = m.Id,
            name = m.Name,
            email = m.Email,
            subject = m.Subject,
            body = m.Body,
            handled = m.Handled,
            createdAt = m.CreatedAt,
        }),
        total = result.Total,
        page = result.Page,
        pageSize = result.PageSize,
    });
}).RequireAuthorization("AdminOnly");

app.MapPost("/api/admin/support/{id:guid}/handled", async (Guid id, ClaimsPrincipal user, ISupportInboxService service) =>
{
    var actorIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
    var actorId = actorIdClaim is null ? (Guid?)null : Guid.Parse(actorIdClaim);
    var result = await service.MarkHandledAsync(id, actorId);
    return result.Status switch
    {
        SupportInboxCommandStatus.Success => Results.Ok(new { id = result.Entity!.Id, handled = result.Entity.Handled }),
        SupportInboxCommandStatus.NotFound => Results.NotFound(),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("AdminOnly");

app.MapPost("/api/admin/email-outbox/process", async (IEmailOutboxProcessor processor) =>
{
    var result = await processor.ProcessPendingAsync();
    return Results.Ok(new { processed = result.Processed, sent = result.Sent, failed = result.Failed });
}).RequireAuthorization("AdminOnly");

app.MapPost("/api/admin/sla/scan", async (ISlaScanOrchestrator orchestrator) =>
{
    var result = await orchestrator.ScanAndEscalateAsync();
    return Results.Ok(new { scanned = result.Scanned, newlyBreached = result.NewlyBreached, approachingBreach = result.ApproachingBreach, escalationsOpened = result.EscalationsOpened });
}).RequireAuthorization("AdminOnly");

// Change 20260726
// SLA policies were seed-only until now; these let an admin retune target hours without a migration.
app.MapGet("/api/admin/sla-policies", async (ISlaPolicyService service) =>
{
    var policies = await service.ListAllAsync();
    return Results.Ok(policies.Select(ToSlaPolicyDto));
}).RequireAuthorization("AdminOnly");

app.MapGet("/api/admin/sla-policies/{id:guid}", async (Guid id, ISlaPolicyService service) =>
{
    var policy = await service.GetAsync(id);
    return policy is null ? Results.NotFound() : Results.Ok(ToSlaPolicyDto(policy));
}).RequireAuthorization("AdminOnly");

app.MapPost("/api/admin/sla-policies", async (SlaPolicyInput input, ClaimsPrincipal user, ISlaPolicyService service) =>
{
    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.CreateAsync(input, actorId);
    return result.Status switch
    {
        SlaPolicyCommandStatus.Success => Results.Ok(ToSlaPolicyDto(result.Entity!)),
        SlaPolicyCommandStatus.DuplicateTransition => Results.BadRequest(new { error = "An SLA policy for this entity type and transition already exists." }),
        SlaPolicyCommandStatus.Invalid => Results.BadRequest(new { error = "Entity type, from state and to state are required; target hours must be positive and warn-at percent between 1 and 100." }),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("AdminOnly");

app.MapPut("/api/admin/sla-policies/{id:guid}", async (Guid id, SlaPolicyInput input, ClaimsPrincipal user, ISlaPolicyService service) =>
{
    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.UpdateAsync(id, input, actorId);
    return result.Status switch
    {
        SlaPolicyCommandStatus.Success => Results.Ok(ToSlaPolicyDto(result.Entity!)),
        SlaPolicyCommandStatus.NotFound => Results.NotFound(),
        SlaPolicyCommandStatus.DuplicateTransition => Results.BadRequest(new { error = "An SLA policy for this entity type and transition already exists." }),
        SlaPolicyCommandStatus.Invalid => Results.BadRequest(new { error = "Entity type, from state and to state are required; target hours must be positive and warn-at percent between 1 and 100." }),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("AdminOnly");

app.MapDelete("/api/admin/sla-policies/{id:guid}", async (Guid id, ClaimsPrincipal user, ISlaPolicyService service) =>
{
    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.DeleteAsync(id, actorId);
    return result.Status switch
    {
        SlaPolicyCommandStatus.Success => Results.NoContent(),
        SlaPolicyCommandStatus.NotFound => Results.NotFound(),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("AdminOnly");

app.MapGet("/api/admin/escalations", async (string? status, string? tier, string? entityType, IEscalationService service) =>
{
    var escalations = await service.ListAsync(new EscalationFilter(status, tier, entityType));
    return Results.Ok(escalations.Select(ToEscalationDto));
}).RequireAuthorization("SupervisorOrAdmin");

app.MapGet("/api/admin/escalations/{id:guid}", async (Guid id, IEscalationService service) =>
{
    var escalation = await service.GetAsync(id);
    if (escalation is null) return Results.NotFound();
    return Results.Ok(new
    {
        id = escalation.Id,
        entityType = escalation.EntityType,
        entityId = escalation.EntityId,
        tierCode = escalation.EscalationTier.Code,
        tierNameEn = escalation.EscalationTier.NameAr,
        reasonAr = escalation.ReasonAr,
        reasonEn = escalation.ReasonEn,
        statusCode = escalation.EscalationStatus.Code,
        statusNameEn = escalation.EscalationStatus.NameAr,
        ownerName = escalation.Owner != null ? escalation.Owner.FullNameAr : null,
        openedAt = escalation.OpenedAt,
        resolutionAr = escalation.ResolutionAr,
        resolutionEn = escalation.ResolutionEn,
    });
}).RequireAuthorization("SupervisorOrAdmin");

app.MapPost("/api/admin/escalations/{id:guid}/acknowledge", async (Guid id, EscalationActionInput input, ClaimsPrincipal user, IEscalationService service) =>
{
    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.AcknowledgeAsync(id, actorId, input.Notes);
    return result.Status switch
    {
        EscalationCommandStatus.Success => Results.Ok(ToEscalationDto(result.Entity!)),
        EscalationCommandStatus.NotFound => Results.NotFound(),
        EscalationCommandStatus.InvalidStatusForAction => Results.BadRequest(new { error = "Escalation cannot be acknowledged in its current status." }),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("SupervisorOrAdmin");

app.MapPost("/api/admin/escalations/{id:guid}/bump", async (Guid id, EscalationActionInput input, ClaimsPrincipal user, IEscalationService service) =>
{
    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.BumpAsync(id, actorId, input.Notes);
    return result.Status switch
    {
        EscalationCommandStatus.Success => Results.Ok(ToEscalationDto(result.Entity!)),
        EscalationCommandStatus.NotFound => Results.NotFound(),
        EscalationCommandStatus.AlreadyMaxTier => Results.BadRequest(new { error = "Escalation is already at the highest tier." }),
        EscalationCommandStatus.InvalidStatusForAction => Results.BadRequest(new { error = "A resolved escalation cannot be bumped." }),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("SupervisorOrAdmin");

app.MapPost("/api/admin/escalations/{id:guid}/resolve", async (Guid id, EscalationResolveInput input, ClaimsPrincipal user, IEscalationService service) =>
{
    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.ResolveAsync(id, actorId, input.ResolutionAr, input.ResolutionEn);
    return result.Status switch
    {
        EscalationCommandStatus.Success => Results.Ok(ToEscalationDto(result.Entity!)),
        EscalationCommandStatus.NotFound => Results.NotFound(),
        EscalationCommandStatus.ResolutionRequired => Results.BadRequest(new { error = "A resolution (in both languages) is required." }),
        EscalationCommandStatus.InvalidStatusForAction => Results.BadRequest(new { error = "Escalation is already resolved." }),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("SupervisorOrAdmin");

app.MapGet("/api/admin/analytics", async (IAnalyticsService service) =>
{
    var kpis = await service.GetPlatformKpisAsync();
    var byStatus = await service.GetIdeasByStatusAsync();
    var submissions = await service.GetSubmissionsOverTimeAsync();
    var themes = await service.GetThemeActivityAsync();
    var evaluators = await service.GetTopEvaluatorsAsync();
    var sla = await service.GetSlaComplianceAsync();

    return Results.Ok(new
    {
        platformKpis = new
        {
            totalIdeas = kpis.TotalIdeas,
            totalApproved = kpis.TotalApproved,
            totalSubmitters = kpis.TotalSubmitters,
            totalEvaluations = kpis.TotalEvaluations,
            totalEvaluators = kpis.TotalEvaluators,
        },
        ideasByStatus = byStatus.Select(s => new { statusCode = s.StatusCode, statusNameEn = s.StatusNameEn, count = s.Count }),
        submissionsOverTime = submissions.Select(s => new { date = s.Date, count = s.Count }),
        themeActivity = themes.Select(t => new { themeNameEn = t.ThemeNameEn, ideaCount = t.IdeaCount, approvedCount = t.ApprovedCount }),
        topEvaluators = evaluators.Select(e => new { evaluatorNameEn = e.EvaluatorNameEn, evaluationCount = e.EvaluationCount, averageScore = e.AverageScore }),
        slaCompliance = new { compliancePct = sla.CompliancePct, totalTracked = sla.TotalTracked },
    });
}).RequireAuthorization("SupervisorOrAdmin");

app.MapGet("/api/analytics/executive", async (IAnalyticsService service) =>
{
    var kpis = await service.GetExtendedPlatformKpisAsync();
    var funnel = await service.GetFunnelAsync();
    var cohort = await service.GetCohortAsync();
    var ideasByStage = await service.GetIdeasByStageAsync();
    var submissions = await service.GetSubmissionsOverTimeFilledAsync();
    var topObjectives = await service.GetTopObjectivesAsync();
    var avgTimePerStage = await service.GetAvgTimePerStageAsync();
    var conversion = await service.GetConversionAsync();

    return Results.Ok(new
    {
        kpis = new
        {
            totalSubmissions = kpis.TotalSubmissions,
            totalApproved = kpis.TotalApproved,
            totalImplemented = kpis.TotalImplemented,
            activeSubmitters = kpis.ActiveSubmitters,
            totalEvaluations = kpis.TotalEvaluations,
            totalUsers = kpis.TotalUsers,
            totalEvaluators = kpis.TotalEvaluators,
            realizedFinancialImpact = kpis.RealizedFinancialImpact,
        },
        funnel = funnel.Select(f => new { stageKey = f.StageKey, count = f.Count }),
        cohort = cohort.Select(c => new { month = c.Month, submitted = c.Submitted, approved = c.Approved, rejected = c.Rejected, implemented = c.Implemented }),
        ideasByStage = ideasByStage.Select(s => new { stage = s.Stage, count = s.Count }),
        submissions = submissions.Select(s => new { date = s.Date, count = s.Count }),
        topObjectives = topObjectives.Select(t => new { themeId = t.ThemeId, nameAr = t.NameAr, nameEn = t.NameEn, count = t.Count }),
        avgTimePerStage = avgTimePerStage.Select(a => new { stage = a.Stage, avgDays = a.AvgDays }),
        conversion = new { submitted = conversion.Submitted, pilot = conversion.Pilot, rate = conversion.Rate },
    });
}).RequireAuthorization("SupervisorOrCommittee");

app.MapGet("/api/analytics/pillars/{themeId:guid}", async (Guid themeId, IAnalyticsService service) =>
{
    var detail = await service.GetPillarDetailAsync(themeId);
    if (detail is null) return Results.NotFound();

    return Results.Ok(new
    {
        themeId = detail.ThemeId,
        nameAr = detail.NameAr,
        nameEn = detail.NameEn,
        descriptionAr = detail.DescriptionAr,
        descriptionEn = detail.DescriptionEn,
        ownerName = detail.OwnerName,
        kpis = new
        {
            ideas = detail.Kpis.Ideas,
            budgetSpent = detail.Kpis.BudgetSpent,
            budgetAllocated = detail.Kpis.BudgetAllocated,
            pilotsActive = detail.Kpis.PilotsActive,
            implementationsDone = detail.Kpis.ImplementationsDone,
        },
        timeline = detail.Timeline.Select(t => new { month = t.Month, count = t.Count }),
        ideas = detail.Ideas.Select(i => new { id = i.Id, code = i.Code, titleAr = i.TitleAr, titleEn = i.TitleEn, status = i.Status, currentStage = i.CurrentStage }),
    });
}).RequireAuthorization("SupervisorOrCommittee");

app.MapGet("/api/platform-stats", async (IAnalyticsService service) =>
{
    var kpis = await service.GetPlatformKpisAsync();
    return Results.Ok(new
    {
        totalIdeas = kpis.TotalIdeas,
        totalApproved = kpis.TotalApproved,
        totalSubmitters = kpis.TotalSubmitters,
        totalEvaluations = kpis.TotalEvaluations,
        totalEvaluators = kpis.TotalEvaluators,
    });
}).RequireAuthorization("AnyAssignedRole");

app.MapGet("/api/dashboard/admin", async (IDashboardService svc) =>
{
    var d = await svc.GetAdminAsync();
    return Results.Ok(new { totalUsers = d.TotalUsers, activeIdeas = d.ActiveIdeas, pendingEvaluations = d.PendingEvaluations, health = d.Health });
}).RequireAuthorization("AdminOnly");

app.MapGet("/api/dashboard/committee", async (IDashboardService svc) =>
{
    var d = await svc.GetCommitteeAsync();
    return Results.Ok(new { awaitingDecision = d.AwaitingDecision, decisionsThisWeek = d.DecisionsThisWeek });
}).RequireAuthorization("SupervisorOrCommittee");

app.MapGet("/api/dashboard/supervisor", async (ClaimsPrincipal principal, IDashboardService svc) =>
{
    var userId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var d = await svc.GetSupervisorAsync(userId);
    return Results.Ok(new
    {
        teamMembers = d.TeamMembers,
        sectorIdeas = d.SectorIdeas,
        escalationsAwaitingMe = d.EscalationsAwaitingMe,
        screening = new
        {
            total = d.Screening.Total,
            underReview = d.Screening.UnderReview,
            approved = d.Screening.Approved,
            returned = d.Screening.Returned,
            rejected = d.Screening.Rejected,
        },
    });
}).RequireAuthorization("SupervisorOrAdmin");

static object ToEscalationDto(Escalation e) => new
{
    id = e.Id,
    entityType = e.EntityType,
    entityId = e.EntityId,
    tierCode = e.EscalationTier.Code,
    tierNameEn = e.EscalationTier.NameAr,
    reasonAr = e.ReasonAr,
    reasonEn = e.ReasonEn,
    statusCode = e.EscalationStatus.Code,
    statusNameEn = e.EscalationStatus.NameAr,
    ownerName = e.Owner != null ? e.Owner.FullNameAr : null,
    openedAt = e.OpenedAt,
};

// Change 20260726
static object ToSlaPolicyDto(SlaPolicy p) => new
{
    id = p.Id,
    entityType = p.EntityType,
    fromState = p.FromState,
    toState = p.ToState,
    targetHours = p.TargetHours,
    warnAtPct = p.WarnAtPct,
};

app.MapPost("/api/admin/invitations/remind", async (IInvitationReminderProcessor processor) =>
{
    var result = await processor.ProcessAsync();
    return Results.Ok(new { scanned = result.Scanned, expired = result.Expired, remindersQueued = result.RemindersQueued });
}).RequireAuthorization("AdminOnly");

app.MapGet("/api/admin/invitations/settings", async (IInvitationReminderSettingsService service) =>
{
    var settings = await service.GetAsync();
    return Results.Ok(new
    {
        enabled = settings.Enabled,
        cronExpression = settings.CronExpression,
        timezone = settings.Timezone,
        stopAfterNReminders = settings.StopAfterNReminders,
        gapHours = settings.GapHours,
        expiresDays = settings.ExpiresDays,
        fromName = settings.FromName,
        fromEmail = settings.FromEmail,
        programNameAr = settings.ProgramNameAr,
        programNameEn = settings.ProgramNameEn,
        updatedAt = settings.UpdatedAt,
    });
}).RequireAuthorization("AdminOnly");

app.MapPatch("/api/admin/invitations/settings", async (InvitationReminderSettingsInput input, ClaimsPrincipal user, IInvitationReminderSettingsService service) =>
{
    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var settings = await service.UpdateAsync(input, actorId);
    return Results.Ok(new
    {
        enabled = settings.Enabled,
        cronExpression = settings.CronExpression,
        timezone = settings.Timezone,
        stopAfterNReminders = settings.StopAfterNReminders,
        gapHours = settings.GapHours,
        expiresDays = settings.ExpiresDays,
        fromName = settings.FromName,
        fromEmail = settings.FromEmail,
        programNameAr = settings.ProgramNameAr,
        programNameEn = settings.ProgramNameEn,
        updatedAt = settings.UpdatedAt,
    });
}).RequireAuthorization("AdminOnly");

app.MapGet("/api/admin/evaluation/settings", async (IEvaluationSettingsService service) =>
{
    var settings = await service.GetAsync();
    return Results.Ok(new { passThreshold = settings.PassThreshold, updatedAt = settings.UpdatedAt });
}).RequireAuthorization("AdminOnly");

app.MapPatch("/api/admin/evaluation/settings", async (EvaluationSettingsPatch input, ClaimsPrincipal user, IEvaluationSettingsService service) =>
{
    if (input.PassThreshold is null) return Results.BadRequest(new { error = "passThreshold is required." });
    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.UpdatePassThresholdAsync(input.PassThreshold.Value, actorId);
    if (!result.Success)
        return Results.BadRequest(new { error = "Passing score must be between 0 and 10." });
    var settings = await service.GetAsync();
    return Results.Ok(new { passThreshold = settings.PassThreshold, updatedAt = settings.UpdatedAt });
}).RequireAuthorization("AdminOnly");

app.MapGet("/api/admin/settings", async (IPlatformSettingsService service) =>
{
    var rows = await service.ListAsync();
    return Results.Ok(rows.Select(r => new { key = r.Key, valueJson = r.ValueJson, updatedAt = r.UpdatedAt }));
}).RequireAuthorization("AdminOnly");

app.MapGet("/api/admin/backup/export", async (ClaimsPrincipal user, IBackupExportService service, IAuditLogWriter auditLogWriter) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.GenerateAsync();

    await auditLogWriter.AppendAsync(
        "backup",
        Guid.Empty,
        "backup.exported",
        userId,
        JsonSerializer.Serialize(new { result.SheetCount, result.RowCount }));

    var fileName = $"backup-{DateTime.UtcNow:yyyyMMdd-HHmmss}.xlsx";
    return Results.File(
        result.Content,
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        fileName);
}).RequireAuthorization("AdminOnly");

app.MapPatch("/api/admin/settings/{key}", async (string key, PlatformSettingPatch input, ClaimsPrincipal user, IPlatformSettingsService service) =>
{
    if (string.IsNullOrEmpty(input.ValueJson))
        return Results.BadRequest(new { error = "valueJson must be valid JSON." });

    try
    {
        using var _ = JsonDocument.Parse(input.ValueJson);
    }
    catch (JsonException)
    {
        return Results.BadRequest(new { error = "valueJson must be valid JSON." });
    }

    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var row = await service.UpdateAsync(key, input.ValueJson, actorId);
    return Results.Ok(new { key = row.Key, valueJson = row.ValueJson, updatedAt = row.UpdatedAt });
}).RequireAuthorization("AdminOnly");

app.MapPost("/api/admin/ideas/{id:guid}/post-program-stage", async (Guid id, PostProgramStageInput input, ClaimsPrincipal user, IPostProgramService service) =>
{
    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.AdvanceAsync(id, input.Stage, actorId, input.Comment);
    return result.Status switch
    {
        PostProgramAdvanceStatus.Success => Results.Ok(new { id = result.Idea!.Id, status = result.Idea.IdeaStatus.Code }),
        PostProgramAdvanceStatus.NotFound => Results.NotFound(),
        PostProgramAdvanceStatus.InvalidStage => Results.BadRequest(new { error = "Invalid post-program stage." }),
        PostProgramAdvanceStatus.InvalidTransition => Results.BadRequest(new { error = "Idea cannot advance to that stage from its current status." }),
        PostProgramAdvanceStatus.CommentRequired => Results.BadRequest(new { error = "A comment is required for a post-program status update." }),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("SupervisorOrAdmin");

app.MapGet("/api/admin/ideas/{id:guid}/post-program-history", async (Guid id, IPostProgramService service) =>
{
    var history = await service.GetHistoryAsync(id);
    return Results.Ok(history.Select(h => new
    {
        fromStage = h.FromStage,
        toStage = h.ToStage,
        comment = h.Comment,
        changedAt = h.ChangedAt,
        changedById = h.ChangedById,
    }));
}).RequireAuthorization("SupervisorOrAdmin");

app.MapGet("/api/admin/post-program/ideas", async (IPostProgramService service) =>
{
    var ideas = await service.GetPostProgramIdeasAsync();
    return Results.Ok(ideas.Select(i => new
    {
        id = i.Id,
        code = i.Code,
        titleAr = i.TitleAr,
        titleEn = i.TitleEn,
        status = i.IdeaStatus.Code,
    }));
}).RequireAuthorization("AdminOnly");

app.MapPost("/api/admin/briefing/generate", async (IWeeklyBriefingProcessor processor) =>
{
    var result = await processor.GenerateAsync();
    return Results.Ok(new
    {
        slaBreachesThisWeek = result.SlaBreachesThisWeek,
        invitationsAcceptedThisWeek = result.InvitationsAcceptedThisWeek,
        pendingInvitations = result.PendingInvitations,
        expiredInvitations = result.ExpiredInvitations,
        auditEntriesThisWeek = result.AuditEntriesThisWeek,
        recipientsQueued = result.RecipientsQueued,
    });
}).RequireAuthorization("AdminOnly");

app.MapHub<NotificationsHub>("/hubs/notifications").RequireAuthorization();

app.MapPost("/api/admin/reports/audit-log/generate", async (ClaimsPrincipal user, IReportGenerationService service) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.GenerateAuditLogReportAsync(userId);
    return Results.Ok(new { reportGenerationId = result.ReportGenerationId, status = result.Status, fileUrl = result.FileUrl });
}).RequireAuthorization("SupervisorOrAdmin");

app.MapPost("/api/admin/reports/ideas/generate", async (ClaimsPrincipal user, IReportGenerationService service) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.GenerateIdeasReportAsync(userId);
    return Results.Ok(new { reportGenerationId = result.ReportGenerationId, status = result.Status, fileUrl = result.FileUrl });
}).RequireAuthorization("SupervisorOrAdmin");

app.MapPost("/api/admin/reports/evaluations/generate", async (ClaimsPrincipal user, IReportGenerationService service) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.GenerateEvaluationsReportAsync(userId);
    return Results.Ok(new { reportGenerationId = result.ReportGenerationId, status = result.Status, fileUrl = result.FileUrl });
}).RequireAuthorization("SupervisorOrAdmin");

// Change 20260726
app.MapGet("/api/reports/evaluator-productivity", async (IEvaluatorProductivityService service) =>
{
    var rows = await service.GetAsync();
    return Results.Ok(rows.Select(r => new
    {
        userId = r.UserId,
        displayName = r.DisplayName,
        assignedCount = r.AssignedCount,
        completedCount = r.CompletedCount,
        draftCount = r.DraftCount,
        avgScore = r.AvgScore,
        avgTurnaroundHours = r.AvgTurnaroundHours,
        coiCount = r.CoiCount,
    }));
}).RequireAuthorization("AdminOnly");

app.MapPost("/api/admin/reports/escalations/generate", async (ClaimsPrincipal user, IReportGenerationService service) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.GenerateEscalationsReportAsync(userId);
    return Results.Ok(new { reportGenerationId = result.ReportGenerationId, status = result.Status, fileUrl = result.FileUrl });
}).RequireAuthorization("SupervisorOrAdmin");

app.MapPost("/api/admin/analytics/export", async (string? format, ClaimsPrincipal user, IReportGenerationService service) =>
{
    var normalizedFormat = format?.ToLowerInvariant();
    if (normalizedFormat is not (ReportFormatCodes.Xlsx or ReportFormatCodes.Pdf or ReportFormatCodes.Pptx))
    {
        return Results.BadRequest(new { error = "format must be one of: xlsx, pdf, pptx." });
    }

    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.GenerateAnalyticsReportAsync(userId, normalizedFormat);
    return Results.Ok(new { reportGenerationId = result.ReportGenerationId, status = result.Status, fileUrl = result.FileUrl });
}).RequireAuthorization("SupervisorOrAdmin");

app.MapPost("/api/admin/reports/generate", async (string type, DateTime? from, DateTime? to, Guid? themeId, string? format, string? locale, ClaimsPrincipal user, IReportGenerationService service) =>
{
    if (!ReportTypeCodes.All.Contains(type)) return Results.BadRequest(new { error = "Unknown report type." });
    var fmt = (format ?? ReportFormatCodes.Xlsx).ToLowerInvariant();
    if (fmt is not (ReportFormatCodes.Xlsx or ReportFormatCodes.Pdf or ReportFormatCodes.Pptx))
    {
        return Results.BadRequest(new { error = "format must be one of: xlsx, pdf, pptx." });
    }
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.GenerateBundleReportAsync(type, from, to, themeId, userId, locale ?? "en", fmt);
    return Results.Ok(new { reportGenerationId = result.ReportGenerationId, status = result.Status, fileUrl = result.FileUrl });
}).RequireAuthorization("SupervisorOrAdmin");

app.MapGet("/api/admin/reports/{id:guid}/download", async (Guid id, InnovationDbContext db, IReportFileStorage storage) =>
{
    var reportGeneration = await db.ReportGenerations.SingleOrDefaultAsync(r => r.Id == id);
    if (reportGeneration is null) return Results.NotFound();
    if (reportGeneration.Status != ReportGenerationStatusCodes.Completed || reportGeneration.FileUrl is null)
    {
        return Results.BadRequest(new { error = "Report is not ready for download." });
    }

    var bytes = await storage.ReadAsync(reportGeneration.FileUrl);
    // Derive the download name from the DB Format field, NOT from the physical stored path: the
    // stored path is now an opaque "{guid}{ext}" (see LocalDiskFileStorage) and no longer carries a
    // meaningful name.
    var fileName = $"report.{reportGeneration.Format}";
    var mimeType = reportGeneration.Format switch
    {
        ReportFormatCodes.Pdf => "application/pdf",
        ReportFormatCodes.Pptx => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        ReportFormatCodes.Xlsx => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        _ => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
    };
    return Results.File(bytes, mimeType, fileName);
}).RequireAuthorization("SupervisorOrAdmin");

app.MapPost("/api/ideas", async (IdeaInput input, ClaimsPrincipal user, IIdeaService service) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.CreateAsync(userId, input);
    return result.Status switch
    {
        IdeaCommandStatus.Success => Results.Created($"/api/ideas/{result.Idea!.Id}", new { id = result.Idea.Id, code = result.Idea.Code, status = IdeaStatusCodes.Draft }),
        IdeaCommandStatus.InvalidStrategicTheme => Results.BadRequest(new { error = "Strategic theme does not exist." }),
        IdeaCommandStatus.InvalidActivity => Results.BadRequest(new { error = "Activity does not exist." }),
        IdeaCommandStatus.InvalidChallenge => Results.BadRequest(new { error = "A valid challenge selection is required for this track." }),
        IdeaCommandStatus.InvalidParticipation => Results.BadRequest(new { error = "Invalid participation type or team roster." }),
        IdeaCommandStatus.TeamMemberNotFound => Results.BadRequest(new { error = "One or more team members could not be found in the directory." }),
        IdeaCommandStatus.ConsentRequired => Results.BadRequest(new { error = "You must acknowledge authorship and agree to the terms." }),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("IdeaAuthor");

app.MapPut("/api/ideas/{id:guid}", async (Guid id, IdeaInput input, ClaimsPrincipal user, IIdeaService service) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.UpdateAsync(id, userId, input);
    return result.Status switch
    {
        IdeaCommandStatus.Success => Results.Ok(new { id = result.Idea!.Id, code = result.Idea.Code }),
        IdeaCommandStatus.NotFound => Results.NotFound(),
        IdeaCommandStatus.Forbidden => Results.StatusCode(StatusCodes.Status403Forbidden),
        IdeaCommandStatus.InvalidState => Results.BadRequest(new { error = "Idea is not in draft state." }),
        IdeaCommandStatus.InvalidStrategicTheme => Results.BadRequest(new { error = "Strategic theme does not exist." }),
        IdeaCommandStatus.InvalidActivity => Results.BadRequest(new { error = "Activity does not exist." }),
        IdeaCommandStatus.InvalidChallenge => Results.BadRequest(new { error = "A valid challenge selection is required for this track." }),
        IdeaCommandStatus.InvalidParticipation => Results.BadRequest(new { error = "Invalid participation type or team roster." }),
        IdeaCommandStatus.TeamMemberNotFound => Results.BadRequest(new { error = "One or more team members could not be found in the directory." }),
        IdeaCommandStatus.ConsentRequired => Results.BadRequest(new { error = "You must acknowledge authorship and agree to the terms." }),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("IdeaAuthor");

app.MapPost("/api/ideas/{id:guid}/submit", async (Guid id, ClaimsPrincipal user, IIdeaService service) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.SubmitAsync(id, userId);
    return result.Status switch
    {
        IdeaCommandStatus.Success => Results.Ok(new { id = result.Idea!.Id, status = result.Idea.IdeaStatus.Code }),
        IdeaCommandStatus.NotFound => Results.NotFound(),
        IdeaCommandStatus.Forbidden => Results.StatusCode(StatusCodes.Status403Forbidden),
        IdeaCommandStatus.InvalidState => Results.BadRequest(new { error = "Idea must be in draft state with at least one attachment to submit." }),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("IdeaAuthor");

app.MapPost("/api/ideas/{id:guid}/resubmit", async (Guid id, IdeaResubmitInput input, ClaimsPrincipal user, IIdeaService service) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.ResubmitAsync(id, userId, input);
    return result.Status switch
    {
        IdeaCommandStatus.Success => Results.Ok(new { id = result.Idea!.Id, status = result.Idea.IdeaStatus.Code }),
        IdeaCommandStatus.NotFound => Results.NotFound(),
        IdeaCommandStatus.Forbidden => Results.StatusCode(StatusCodes.Status403Forbidden),
        IdeaCommandStatus.InvalidState => Results.BadRequest(new { error = "Idea is not in returned state." }),
        IdeaCommandStatus.SectionNotEditable => Results.StatusCode(StatusCodes.Status403Forbidden),
        IdeaCommandStatus.InvalidStrategicTheme => Results.BadRequest(new { error = "Strategic theme does not exist." }),
        IdeaCommandStatus.InvalidActivity => Results.BadRequest(new { error = "Activity does not exist." }),
        IdeaCommandStatus.InvalidChallenge => Results.BadRequest(new { error = "A valid challenge selection is required for this track." }),
        IdeaCommandStatus.InvalidParticipation => Results.BadRequest(new { error = "Invalid participation type or team roster." }),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("IdeaAuthor");

app.MapGet("/api/ideas", async (string? q, Guid? strategicThemeId, Guid? activityId, string? status, int? stage, int? page, int? pageSize, ClaimsPrincipal user, IIdeaService service) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var email = user.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
    var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
    var callerSam = CallerIdentity.ResolveSam(user);
    var filter = new IdeaListFilter(q, strategicThemeId, activityId, status, stage, page ?? 1, pageSize ?? 25);
    var result = await service.ListAsync(filter, userId, email, roles, callerSam);
    return Results.Ok(new
    {
        items = result.Items.Select(i => new { id = i.Id, code = i.Code, titleAr = i.TitleAr, titleEn = i.TitleEn, problemStatementAr = i.ProblemStatementAr, problemStatementEn = i.ProblemStatementEn, currentStage = i.CurrentStage, status = i.Status, strategicThemeId = i.StrategicThemeId, activityId = i.ActivityId }),
        total = result.Total, page = result.Page, pageSize = result.PageSize,
    });
}).RequireAuthorization("AnyAssignedRole");

app.MapGet("/api/search", async (string? q, ClaimsPrincipal user, ISearchService service) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var email = user.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
    var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
    var results = await service.SearchAsync(q ?? string.Empty, userId, email, roles);
    return Results.Ok(results);
}).RequireAuthorization("AnyAssignedRole");

app.MapGet("/api/directory/search", async (string? q, int? limit, IAdIdentityLookupService lookup, CancellationToken ct) =>
{
    var trimmed = q?.Trim() ?? string.Empty;
    if (trimmed.Length < 2)
    {
        return Results.BadRequest(new { error = "Query must be at least 2 characters." });
    }

    var clampedLimit = Math.Clamp(limit ?? 10, 1, 25);
    var identities = await lookup.SearchByNameAsync(trimmed, clampedLimit, ct);
    var results = identities.Select(i => new DirectoryPersonDto(i.SamAccountName, i.DisplayName, i.Email));
    return Results.Ok(results);
}).RequireAuthorization("AnyAssignedRole");

app.MapGet("/api/ideas/mine", async (string? statusGroup, string? status, string? sort, int? page, int? size, ClaimsPrincipal user, IIdeaService service) => // Change 20260726
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var email = user.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
    var callerSam = CallerIdentity.ResolveSam(user);
    var ideas = await service.GetMineDetailedAsync(userId, statusGroup, email, callerSam);
    if (!string.IsNullOrWhiteSpace(status)) // Change 20260726
    { // Change 20260726
        ideas = ideas.Where(i => string.Equals(i.Status, status, StringComparison.OrdinalIgnoreCase)).ToList(); // Change 20260726
    } // Change 20260726

    ideas = (sort?.Trim().ToLowerInvariant() switch // Change 20260726
    { // Change 20260726
        "createdat asc" => ideas.OrderBy(i => i.CreatedAt), // Change 20260726
        "createdat desc" => ideas.OrderByDescending(i => i.CreatedAt), // Change 20260726
        "updatedat asc" => ideas.OrderBy(i => i.UpdatedAt), // Change 20260726
        "updatedat desc" => ideas.OrderByDescending(i => i.UpdatedAt), // Change 20260726
        _ => ideas.OrderByDescending(i => i.CreatedAt), // Change 20260726
    }).ToList(); // Change 20260726

    var total = ideas.Count; // Change 20260726
    // Pagination is opt-in: callers that pass neither page nor size still get the bare array the
    // existing clients expect, while paginating callers get the {items,total,page,size} envelope.
    var paginate = page.HasValue || size.HasValue; // Change 20260726
    var effectivePage = Math.Max(page ?? 1, 1); // Change 20260726
    var effectiveSize = Math.Clamp(size ?? 20, 1, 200); // Change 20260726
    var pageItems = paginate // Change 20260726
        ? ideas.Skip((effectivePage - 1) * effectiveSize).Take(effectiveSize).ToList() // Change 20260726
        : ideas; // Change 20260726

    var items = pageItems.Select(i => new // Change 20260726
    {
        id = i.Id,
        code = i.Code,
        titleAr = i.TitleAr,
        titleEn = i.TitleEn,
        themeId = i.StrategicThemeId, // Change 20260726
        themeNameAr = i.ThemeNameAr, // Change 20260726
        themeNameEn = i.ThemeNameEn, // Change 20260726
        status = i.Status,
        currentStage = i.CurrentStage,
        createdAt = i.CreatedAt,
        updatedAt = i.UpdatedAt,
        feedbackCount = i.FeedbackCount,
        isOwner = i.IsOwner,
    }).ToList(); // Change 20260726

    return paginate // Change 20260726
        ? Results.Ok(new { items, total, page = effectivePage, size = effectiveSize }) // Change 20260726
        : Results.Ok(items); // Change 20260726
}).RequireAuthorization("AnyAssignedRole");

app.MapGet("/api/ideas/{id:guid}", async (Guid id, ClaimsPrincipal user, IIdeaService service, InnovationDbContext db) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var isElevatedReviewer = user.IsInRole(RoleCodes.Evaluator) || user.IsInRole(RoleCodes.Judge) || user.IsInRole(RoleCodes.Supervisor) || user.IsInRole(RoleCodes.Admin);
    // An "evaluator-only" caller (evaluator role, but not also judge/supervisor/admin) must never see
    // submitter/team identity, and may only fetch ideas they are assigned to evaluate (Rule B/D).
    var isEvaluatorOnly = user.IsInRole(RoleCodes.Evaluator)
        && !user.IsInRole(RoleCodes.Judge) && !user.IsInRole(RoleCodes.Supervisor) && !user.IsInRole(RoleCodes.Admin);
    if (isEvaluatorOnly)
    {
        var assignedToMe = await db.Set<Assignment>().AnyAsync(a => a.IdeaId == id && a.EvaluatorId == userId && a.Kind == AssignmentKinds.Evaluator);
        if (!assignedToMe) return Results.StatusCode(StatusCodes.Status403Forbidden);
    }
    var callerSam = CallerIdentity.ResolveSam(user);
    var result = await service.GetByIdAsync(id, userId, isElevatedReviewer, callerSam);
    if (result.Status == IdeaCommandStatus.NotFound) return Results.NotFound();
    if (result.Status == IdeaCommandStatus.Forbidden) return Results.StatusCode(StatusCodes.Status403Forbidden);

    var attachmentsResult = await service.GetAttachmentsAsync(id, userId, isElevatedReviewer, callerSam);
    var idea = result.Idea!;
    // Status history comes from the hash-chained audit log; actions are namespaced "idea.*".
    var auditTrail = await db.Set<AuditLog>() // Change 20260726
        .Where(a => a.EntityType == "idea" && a.EntityId == id) // Change 20260726
        .OrderBy(a => a.OccurredAt) // Change 20260726
        .Select(a => new { action = a.Action, actorId = a.ActorId, occurredAt = a.OccurredAt, payload = a.Payload }) // Change 20260726
        .ToListAsync(); // Change 20260726
    return Results.Ok(new
    {
        id = idea.Id,
        code = idea.Code,
        submitterId = isEvaluatorOnly ? (Guid?)null : idea.SubmitterId,
        // The submitter is the idea's team lead; surface their identity so the detail page can show
        // them at the head of the roster. Hidden from evaluator-only reviewers, same as the roster.
        submitter = isEvaluatorOnly || idea.Submitter is null
            ? null
            : new { name = idea.Submitter.FullNameAr, samAccountName = idea.Submitter.SamAccountName, email = idea.Submitter.Email },
        titleAr = idea.TitleAr,
        titleEn = idea.TitleEn,
        problemStatementAr = idea.ProblemStatementAr,
        problemStatementEn = idea.ProblemStatementEn,
        proposedSolutionAr = idea.ProposedSolutionAr,
        proposedSolutionEn = idea.ProposedSolutionEn,
        expectedBenefitsAr = idea.ExpectedBenefitsAr,
        expectedBenefitsEn = idea.ExpectedBenefitsEn,
        strategicThemeId = idea.StrategicThemeId,
        activityId = idea.ActivityId,
        challengeId = idea.ChallengeId,
        participationType = idea.ParticipationType,
        teamName = isEvaluatorOnly ? null : idea.TeamName,
        teamMembers = idea.TeamMembers.Where(m => !isEvaluatorOnly).OrderBy(m => m.SortOrder).Select(m => new { m.Id, m.Name, m.Email, m.SamAccountName }),
        ipAcknowledged = idea.IpAcknowledged,
        termsAgreed = idea.TermsAgreed,
        editableSections = idea.EditableSections,
        status = idea.IdeaStatus.Code,
        screeningReason = idea.ScreeningReason,
        currentStage = idea.CurrentStage,
        createdAt = idea.CreatedAt,
        updatedAt = idea.UpdatedAt,
        approvedAt = idea.ApprovedAt,
        attachments = attachmentsResult.Attachments.Select(a => new { id = a.Id, fileName = a.FileName, contentType = a.ContentType, fileSizeBytes = a.FileSizeBytes, uploadedAt = a.UploadedAt }),
        auditTrail, // Change 20260726
    });
}).RequireAuthorization("AnyAssignedRole");

app.MapPost("/api/ideas/{id:guid}/attachments", async (Guid id, IFormFile file, ClaimsPrincipal user, IIdeaService service) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    using var stream = new MemoryStream();
    await file.CopyToAsync(stream);
    var content = stream.ToArray();

    var result = await service.AddAttachmentAsync(id, userId, file.FileName, file.ContentType, content);
    return result.Status switch
    {
        IdeaCommandStatus.Success => Results.Ok(new { id = result.Attachment!.Id, fileName = result.Attachment.FileName }),
        IdeaCommandStatus.NotFound => Results.NotFound(),
        IdeaCommandStatus.Forbidden => Results.StatusCode(StatusCodes.Status403Forbidden),
        IdeaCommandStatus.InvalidState => Results.BadRequest(new { error = "Idea is not in draft state." }),
        IdeaCommandStatus.InvalidAttachment => Results.BadRequest(new { error = "Unsupported file type or file exceeds the size limit." }),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("IdeaAuthor").DisableAntiforgery();

app.MapGet("/api/ideas/{id:guid}/attachments", async (Guid id, ClaimsPrincipal user, IIdeaService service) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var isElevatedReviewer = user.IsInRole(RoleCodes.Evaluator) || user.IsInRole(RoleCodes.Judge) || user.IsInRole(RoleCodes.Supervisor) || user.IsInRole(RoleCodes.Admin);
    var callerSam = CallerIdentity.ResolveSam(user);
    var result = await service.GetAttachmentsAsync(id, userId, isElevatedReviewer, callerSam);
    return result.Status switch
    {
        IdeaCommandStatus.Success => Results.Ok(result.Attachments.Select(a => new { id = a.Id, fileName = a.FileName, contentType = a.ContentType, fileSizeBytes = a.FileSizeBytes, uploadedAt = a.UploadedAt })),
        IdeaCommandStatus.NotFound => Results.NotFound(),
        IdeaCommandStatus.Forbidden => Results.StatusCode(StatusCodes.Status403Forbidden),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("AnyAssignedRole");

app.MapDelete("/api/ideas/{id:guid}/attachments/{attachmentId:guid}", async (Guid id, Guid attachmentId, ClaimsPrincipal user, IIdeaService service) => // Change 20260726
{ // Change 20260726
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!); // Change 20260726
    var result = await service.DeleteAttachmentAsync(id, attachmentId, userId); // Change 20260726
    return result.Status switch // Change 20260726
    { // Change 20260726
        IdeaCommandStatus.Success => Results.Ok(result.Attachments.Select(a => new { id = a.Id, fileName = a.FileName, contentType = a.ContentType, fileSizeBytes = a.FileSizeBytes, uploadedAt = a.UploadedAt })), // Change 20260726
        IdeaCommandStatus.NotFound => Results.NotFound(), // Change 20260726
        IdeaCommandStatus.Forbidden => Results.StatusCode(StatusCodes.Status403Forbidden), // Change 20260726
        IdeaCommandStatus.InvalidState => Results.Conflict(new { error = "Attachments cannot be removed in the idea's current state." }), // Change 20260726
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError), // Change 20260726
    }; // Change 20260726
}).RequireAuthorization("IdeaAuthor"); // Change 20260726

app.MapGet("/api/ideas/{id:guid}/attachments/{attachmentId:guid}", async (Guid id, Guid attachmentId, ClaimsPrincipal user, IIdeaService service) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var isElevatedReviewer = user.IsInRole(RoleCodes.Evaluator) || user.IsInRole(RoleCodes.Judge) || user.IsInRole(RoleCodes.Supervisor) || user.IsInRole(RoleCodes.Admin);
    var callerSam = CallerIdentity.ResolveSam(user);
    var result = await service.GetAttachmentFileAsync(id, attachmentId, userId, isElevatedReviewer, callerSam);
    return result.Status switch
    {
        IdeaCommandStatus.Success => Results.File(result.Content!, result.ContentType, result.FileName),
        IdeaCommandStatus.NotFound => Results.NotFound(),
        IdeaCommandStatus.Forbidden => Results.StatusCode(StatusCodes.Status403Forbidden),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("AnyAssignedRole");

app.MapPost("/api/admin/idea-template", async (IFormFile? file, ClaimsPrincipal user, IIdeaTemplateService service) =>
{
    if (file is null || file.Length == 0)
        return Results.BadRequest(new { error = "A file is required." });

    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    await using var stream = file.OpenReadStream();
    var template = await service.ReplaceAsync(stream, file.FileName, file.ContentType, actorId);

    return Results.Ok(new
    {
        fileName = template.FileName,
        contentType = template.ContentType,
        sizeBytes = template.SizeBytes,
        uploadedAt = template.UploadedAt,
    });
}).RequireAuthorization("AdminOnly").DisableAntiforgery();

app.MapGet("/api/admin/idea-template", async (IIdeaTemplateService service) =>
{
    var current = await service.GetCurrentAsync();
    if (current is null) return Results.NotFound();

    return Results.Ok(new
    {
        fileName = current.FileName,
        contentType = current.ContentType,
        sizeBytes = current.SizeBytes,
        uploadedAt = current.UploadedAt,
    });
}).RequireAuthorization("AdminOnly");

app.MapGet("/api/idea-template/current", async (IIdeaTemplateService service, IIdeaTemplateFileStorage storage) =>
{
    var current = await service.GetCurrentAsync();
    if (current is null) return Results.NotFound();

    var bytes = await storage.ReadAsync(current.StoredPath);
    return Results.File(bytes, current.ContentType, current.FileName);
}).RequireAuthorization("AnyAssignedRole");

app.MapPost("/api/admin/home/media", async (IFormFile? file, ClaimsPrincipal user, IHomeMediaService service) =>
{
    if (file is null || file.Length == 0)
        return Results.BadRequest(new { error = "A file is required." });
    if (!HomeMediaRules.IsAllowed(file.ContentType))
        return Results.BadRequest(new { error = "Unsupported file type." });
    if (file.Length > HomeMediaRules.MaxSizeFor(file.ContentType))
        return Results.BadRequest(new { error = "File is too large." });

    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    await using var stream = file.OpenReadStream();
    var media = await service.SaveAsync(stream, file.FileName, file.ContentType, actorId);

    return Results.Ok(new { id = media.Id, url = $"/api/public/home/media/{media.Id}" });
}).RequireAuthorization("AdminOnly").DisableAntiforgery();

app.MapGet("/api/public/home/media/{id:guid}", async (Guid id, IHomeMediaService service, IHomeMediaFileStorage storage) =>
{
    var media = await service.GetAsync(id);
    if (media is null) return Results.NotFound();
    var bytes = await storage.ReadAsync(media.StoredPath);
    return Results.File(bytes, media.ContentType);
});

app.MapGet("/api/strategic-themes", async (InnovationDbContext db) =>
{
    var themes = await db.StrategicThemes
        .OrderBy(t => t.Priority)
        .Select(t => new { id = t.Id, nameAr = t.NameAr, nameEn = t.NameEn, descriptionAr = t.DescriptionAr, descriptionEn = t.DescriptionEn })
        .ToListAsync();
    return Results.Ok(themes);
}).RequireAuthorization("AnyAssignedRole");

app.MapPost("/api/strategic-themes", async (StrategicThemeInput input, ClaimsPrincipal user, IStrategicThemeService service) =>
{
    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.CreateAsync(input, actorId);
    return result.Status switch
    {
        StrategicThemeCommandStatus.Success => Results.Created($"/api/strategic-themes/{result.Entity!.Id}", new { id = result.Entity.Id, nameAr = result.Entity.NameAr, nameEn = result.Entity.NameEn, descriptionAr = result.Entity.DescriptionAr, descriptionEn = result.Entity.DescriptionEn }),
        StrategicThemeCommandStatus.InvalidInput => Results.BadRequest(new { error = "Arabic and English names are required." }),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("SupervisorOrAdmin");

app.MapPatch("/api/strategic-themes/{id:guid}", async (Guid id, StrategicThemeInput input, ClaimsPrincipal user, IStrategicThemeService service) =>
{
    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.UpdateAsync(id, input, actorId);
    return result.Status switch
    {
        StrategicThemeCommandStatus.Success => Results.Ok(new { id = result.Entity!.Id, nameAr = result.Entity.NameAr, nameEn = result.Entity.NameEn, descriptionAr = result.Entity.DescriptionAr, descriptionEn = result.Entity.DescriptionEn }),
        StrategicThemeCommandStatus.NotFound => Results.NotFound(),
        StrategicThemeCommandStatus.InvalidInput => Results.BadRequest(new { error = "Arabic and English names are required." }),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("SupervisorOrAdmin");

app.MapDelete("/api/strategic-themes/{id:guid}", async (Guid id, ClaimsPrincipal user, IStrategicThemeService service) =>
{
    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.DeleteAsync(id, actorId);
    return result.Status switch
    {
        StrategicThemeCommandStatus.Success => Results.NoContent(),
        StrategicThemeCommandStatus.NotFound => Results.NotFound(),
        StrategicThemeCommandStatus.InUse => Results.Conflict(new { error = "Cannot delete a track that ideas currently reference. Reassign those ideas to another track first." }),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("SupervisorOrAdmin");

app.MapGet("/api/admin/email-templates", async (IEmailTemplateService service) =>
{
    var templates = await service.ListAsync();
    return Results.Ok(templates.Select(t => new { id = t.Id, kind = t.Kind, subjectAr = t.SubjectAr, subjectEn = t.SubjectEn, bodyAr = t.BodyAr, bodyEn = t.BodyEn, isBroadcast = t.IsBroadcast }));
}).RequireAuthorization("SupervisorOrAdmin");

app.MapPatch("/api/admin/email-templates/{id:guid}", async (Guid id, EmailTemplateInput input, ClaimsPrincipal user, IEmailTemplateService service) =>
{
    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.UpdateAsync(id, input, actorId);
    return result.Status switch
    {
        EmailTemplateCommandStatus.Success => Results.Ok(new { id = result.Entity!.Id, kind = result.Entity.Kind, subjectAr = result.Entity.SubjectAr, subjectEn = result.Entity.SubjectEn, bodyAr = result.Entity.BodyAr, bodyEn = result.Entity.BodyEn, isBroadcast = result.Entity.IsBroadcast }),
        EmailTemplateCommandStatus.NotFound => Results.NotFound(),
        EmailTemplateCommandStatus.InvalidInput => Results.BadRequest(new { error = "Subject and body are required in both languages." }),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("SupervisorOrAdmin");

app.MapGet("/api/admin/email-templates/{templateId:guid}/attachments", async (Guid templateId, IEmailTemplateAttachmentService service) =>
{
    var attachments = await service.ListByTemplateAsync(templateId);
    return Results.Ok(attachments.Select(a => new { id = a.Id, fileName = a.FileName, contentType = a.ContentType, fileSizeBytes = a.FileSizeBytes, uploadedAt = a.UploadedAt }));
}).RequireAuthorization("SupervisorOrAdmin");

app.MapPost("/api/admin/email-templates/{templateId:guid}/attachments", async (Guid templateId, IFormFile file, ClaimsPrincipal user, IEmailTemplateAttachmentService service) =>
{
    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    using var stream = new MemoryStream();
    await file.CopyToAsync(stream);
    var content = stream.ToArray();

    var result = await service.UploadAsync(templateId, file.FileName, file.ContentType, content, actorId);
    return result.Status switch
    {
        EmailTemplateAttachmentCommandStatus.Success => Results.Ok(new { id = result.Entity!.Id, fileName = result.Entity.FileName, contentType = result.Entity.ContentType, fileSizeBytes = result.Entity.FileSizeBytes, uploadedAt = result.Entity.UploadedAt }),
        EmailTemplateAttachmentCommandStatus.TemplateNotFound => Results.NotFound(),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("SupervisorOrAdmin").DisableAntiforgery();

app.MapDelete("/api/admin/email-template-attachments/{id:guid}", async (Guid id, ClaimsPrincipal user, IEmailTemplateAttachmentService service) =>
{
    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.DeleteAsync(id, actorId);
    return result.Status switch
    {
        EmailTemplateAttachmentCommandStatus.Success => Results.NoContent(),
        EmailTemplateAttachmentCommandStatus.NotFound => Results.NotFound(),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("SupervisorOrAdmin");

app.MapGet("/api/admin/assignments", async (Guid? evaluatorId, string? status, string? ideaSearch, int? page, int? pageSize, IAssignmentService service) =>
{
    var filter = new AssignmentListFilter(evaluatorId, status, ideaSearch, page ?? 1, pageSize ?? 25);
    var result = await service.ListAsync(filter);
    return Results.Ok(new
    {
        items = result.Items.Select(a => new
        {
            id = a.Id,
            ideaId = a.IdeaId,
            ideaCode = a.Idea.Code,
            ideaTitleAr = a.Idea.TitleAr,
            ideaTitleEn = a.Idea.TitleEn,
            evaluatorId = a.EvaluatorId,
            evaluatorName = a.Evaluator.FullNameAr,
            assignedAt = a.AssignedAt,
            dueAt = a.DueAt,
            statusCode = a.AssignmentStatus.Code,
            notes = a.Notes,
        }),
        total = result.Total,
        page = result.Page,
        pageSize = result.PageSize,
    });
}).RequireAuthorization("SupervisorOrAdmin");

app.MapGet("/api/admin/assignments/workload-heatmap", async (IAssignmentService service) =>
{
    var rows = await service.GetWorkloadHeatmapAsync();
    return Results.Ok(rows.Select(r => new { evaluatorId = r.EvaluatorId, evaluatorName = r.EvaluatorName, pending = r.Pending, dueSoon = r.DueSoon, overdue = r.Overdue, completedRecent = r.CompletedRecent }));
}).RequireAuthorization("SupervisorOrAdmin");

app.MapGet("/api/admin/assignments/suggest-evaluators", async (IAssignmentService service) =>
{
    var suggestions = await service.SuggestLeastLoadedEvaluatorsAsync();
    return Results.Ok(suggestions.Select(s => new { evaluatorId = s.EvaluatorId, evaluatorName = s.EvaluatorName, openCount = s.OpenCount }));
}).RequireAuthorization("SupervisorOrAdmin");

app.MapGet("/api/admin/assignments/idea-options", async (IAssignmentService service) =>
{
    var options = await service.ListIdeaOptionsAsync();
    return Results.Ok(options.Select(o => new { id = o.Id, code = o.Code, titleAr = o.TitleAr, titleEn = o.TitleEn }));
}).RequireAuthorization("SupervisorOrAdmin");

app.MapPost("/api/admin/assignments", async (AssignmentCreateInput input, ClaimsPrincipal user, IAssignmentService service, ISlaClockService slaClockService) =>
{
    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.CreateAsync(input, actorId);
    if (result.Status == AssignmentCommandStatus.Success)
    {
        await slaClockService.OpenAsync("assignment", result.Entity!.Id);
        return Results.Created($"/api/admin/assignments/{result.Entity.Id}", new { id = result.Entity.Id, statusCode = result.Entity.AssignmentStatus.Code });
    }
    return result.Status switch
    {
        AssignmentCommandStatus.InvalidIdea => Results.BadRequest(new { error = "Idea does not exist." }),
        AssignmentCommandStatus.InvalidEvaluator => Results.BadRequest(new { error = "Selected user does not hold the evaluator role." }),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("SupervisorOrAdmin");

app.MapPost("/api/admin/assignments/bulk", async (BulkAssignmentCreateRequest input, ClaimsPrincipal user, IAssignmentService service, ISlaClockService slaClockService) =>
{
    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var results = await service.BulkCreateAsync(input.Assignments, actorId);
    foreach (var result in results.Where(r => r.Status == AssignmentCommandStatus.Success))
    {
        await slaClockService.OpenAsync("assignment", result.Entity!.Id);
    }
    return Results.Ok(new { created = results.Select(r => new { status = r.Status.ToString(), id = r.Entity?.Id }) });
}).RequireAuthorization("SupervisorOrAdmin");

app.MapPatch("/api/admin/assignments/{id:guid}", async (Guid id, AssignmentUpdateInput input, ClaimsPrincipal user, IAssignmentService service, ISlaClockService slaClockService) =>
{
    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.UpdateAsync(id, input, actorId);
    if (result.Status == AssignmentCommandStatus.Success)
    {
        if (input.StatusCode == "completed")
        {
            await slaClockService.CloseAsync("assignment", id);
        }
        return Results.Ok(new { id = result.Entity!.Id, statusCode = result.Entity.AssignmentStatus.Code, evaluatorId = result.Entity.EvaluatorId, dueAt = result.Entity.DueAt, notes = result.Entity.Notes });
    }
    return Results.NotFound();
}).RequireAuthorization("SupervisorOrAdmin");

app.MapDelete("/api/admin/assignments/{id:guid}", async (Guid id, ClaimsPrincipal user, IAssignmentService service, ISlaClockService slaClockService) =>
{
    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.UnassignAsync(id, actorId);
    if (result.Status == AssignmentCommandStatus.Success)
    {
        await slaClockService.CloseAsync("assignment", id);
        return Results.NoContent();
    }
    return Results.NotFound();
}).RequireAuthorization("SupervisorOrAdmin");

app.MapPost("/api/admin/assignments/bulk-unassign", async (BulkUnassignRequest input, ClaimsPrincipal user, IAssignmentService service, ISlaClockService slaClockService) =>
{
    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var results = await service.BulkUnassignAsync(input.Ids, actorId);
    var successIds = new List<Guid>();
    foreach (var result in results.Where(r => r.Status == AssignmentCommandStatus.Success))
    {
        await slaClockService.CloseAsync("assignment", result.Entity!.Id);
        successIds.Add(result.Entity.Id);
    }
    return Results.Ok(new { unassigned = successIds.Count });
}).RequireAuthorization("SupervisorOrAdmin");

app.MapGet("/api/admin/roster", async (IRosterService service) =>
{
    var rows = await service.GetHubAsync();
    return Results.Ok(rows.Select(r => new
    {
        roleCode = r.RoleCode,
        roleNameAr = r.RoleNameAr,
        roleNameEn = r.RoleNameEn,
        activeCount = r.ActiveCount,
        pendingCount = r.PendingCount,
        expiredCount = r.ExpiredCount,
        withdrawnCount = r.WithdrawnCount,
    }));
}).RequireAuthorization("SupervisorOrAdmin");

app.MapGet("/api/admin/roster/{roleCode}", async (string roleCode, IRosterService service) =>
{
    var detail = await service.GetRoleDetailAsync(roleCode);
    if (detail is null) return Results.NotFound();
    return Results.Ok(new
    {
        roleCode = detail.RoleCode,
        roleNameAr = detail.RoleNameAr,
        roleNameEn = detail.RoleNameAr,
        activeMembers = detail.ActiveMembers.Select(m => new
        {
            userId = m.UserId,
            samAccountName = m.SamAccountName,
            fullNameAr = m.FullNameAr,
            fullNameEn = m.FullNameEn,
            email = m.Email,
            isActive = m.IsActive,
        }),
        invitations = detail.Invitations.Select(i => new
        {
            id = i.Id,
            samAccountName = i.SamAccountName,
            displayName = i.DisplayName,
            email = i.Email,
            status = i.RoleInvitationStatus.Code,
            deadlineAt = i.DeadlineAt,
            respondedAt = i.RespondedAt,
            reminderCount = i.ReminderCount,
            lastReminderAt = i.LastReminderAt,
            source = i.Source,
            invitedByName = i.InvitedBy.FullNameAr,
            createdAt = i.CreatedAt,
        }),
    });
}).RequireAuthorization("SupervisorOrAdmin");

app.MapGet("/api/admin/roster/settings", async (IRoleInvitationSettingsService service) =>
{
    var settings = await service.GetAsync();
    return Results.Ok(new
    {
        enabled = settings.Enabled,
        defaultExpiresDays = settings.DefaultExpiresDays,
        reminderGapHours = settings.ReminderGapHours,
        maxReminders = settings.MaxReminders,
        updatedAt = settings.UpdatedAt,
    });
}).RequireAuthorization("AdminOnly");

app.MapPost("/api/admin/roster/{roleCode}/invite", async (string roleCode, RosterInviteRequest input, ClaimsPrincipal user, IRosterService service) =>
{
    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var inputs = input.SamAccountNames.Select(s => new RoleInvitationCreateInput(s, roleCode, input.DeadlineAt, "manual")).ToList();
    var results = await service.BulkCreateInvitationsAsync(inputs, actorId);
    return Results.Ok(new
    {
        total = results.Count,
        created = results.Count(r => r.Status == RoleInvitationCommandStatus.Success),
        skipped = results.Count(r => r.Status != RoleInvitationCommandStatus.Success),
        errors = results
            .Where(r => r.Status != RoleInvitationCommandStatus.Success)
            .Select(r => new { samAccountName = r.SamAccountName, message = r.Status.ToString() }),
    });
}).RequireAuthorization("SupervisorOrAdmin");

app.MapPost("/api/admin/roster/{id:guid}/withdraw", async (Guid id, ClaimsPrincipal user, IRosterService service) =>
{
    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.WithdrawAsync(id, actorId);
    return result.Status switch
    {
        RoleInvitationCommandStatus.Success => Results.Ok(new { id = result.Entity!.Id, status = "withdrawn" }),
        RoleInvitationCommandStatus.NotFound => Results.NotFound(),
        RoleInvitationCommandStatus.InvalidStatus => Results.Conflict(new { error = "Only pending invitations can be withdrawn." }),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("SupervisorOrAdmin");

app.MapPost("/api/admin/roster/withdraw-bulk", async (RosterBulkIdRequest input, ClaimsPrincipal user, IRosterService service) =>
{
    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var results = await service.BulkWithdrawAsync(input.Ids, actorId);
    return Results.Ok(new { withdrawn = results.Count(r => r.Status == RoleInvitationCommandStatus.Success) });
}).RequireAuthorization("SupervisorOrAdmin");

app.MapPost("/api/admin/roster/{id:guid}/remind", async (Guid id, IRosterService service) =>
{
    var result = await service.RemindAsync(id);
    return result.Status switch
    {
        RoleInvitationCommandStatus.Success => Results.Ok(new { id = result.Entity!.Id, reminderCount = result.Entity.ReminderCount }),
        RoleInvitationCommandStatus.NotFound => Results.NotFound(),
        RoleInvitationCommandStatus.InvalidStatus => Results.Conflict(new { error = "Invitation is not eligible for a reminder." }),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("SupervisorOrAdmin");

app.MapPost("/api/admin/roster/remind-bulk", async (RosterBulkIdRequest input, IRosterService service) =>
{
    var results = await service.BulkRemindAsync(input.Ids);
    return Results.Ok(new { reminded = results.Count(r => r.Status == RoleInvitationCommandStatus.Success) });
}).RequireAuthorization("SupervisorOrAdmin");

app.MapPatch("/api/admin/roster/settings", async (RoleInvitationSettingsInput input, ClaimsPrincipal user, IRoleInvitationSettingsService service) =>
{
    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var settings = await service.UpdateAsync(input, actorId);
    return Results.Ok(new
    {
        enabled = settings.Enabled,
        defaultExpiresDays = settings.DefaultExpiresDays,
        reminderGapHours = settings.ReminderGapHours,
        maxReminders = settings.MaxReminders,
        updatedAt = settings.UpdatedAt,
    });
}).RequireAuthorization("AdminOnly");

app.MapPost("/api/admin/employees/import", async (EmployeeImportRequest input, ClaimsPrincipal user, IRosterService service) =>
{
    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var inputs = input.Rows.Select(r => new RoleInvitationCreateInput(r.SamAccountName, r.RoleCode, null, "import")).ToList();
    var results = await service.BulkCreateInvitationsAsync(inputs, actorId);
    return Results.Ok(new
    {
        total = results.Count,
        created = results.Count(r => r.Status == RoleInvitationCommandStatus.Success),
        skipped = results.Count(r => r.Status != RoleInvitationCommandStatus.Success),
        errors = results
            .Where(r => r.Status != RoleInvitationCommandStatus.Success)
            .Select(r => new { samAccountName = r.SamAccountName, message = r.Status.ToString() }),
    });
}).RequireAuthorization("SupervisorOrAdmin");

app.MapGet("/api/admin/phases", async (IPhaseScheduleService service) =>
{
    var phases = await service.ListAsync();
    return Results.Ok(phases.Select(p => new { idx = p.Idx, code = p.Code, labelAr = p.LabelAr, labelEn = p.LabelEn, startsAt = p.StartsAt, endsAt = p.EndsAt, updatedAt = p.UpdatedAt, announcedAt = p.AnnouncedAt }));
}).RequireAuthorization("SupervisorOrAdmin");

app.MapPatch("/api/admin/phases/{idx:int}", async (int idx, PhaseScheduleInput input, ClaimsPrincipal user, IPhaseScheduleService service) =>
{
    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.UpdateAsync(idx, input.StartsAt, input.EndsAt, actorId);
    return result.Status switch
    {
        PhaseScheduleCommandStatus.Success => Results.Ok(new { idx = result.Entity!.Idx, code = result.Entity.Code, labelAr = result.Entity.LabelAr, labelEn = result.Entity.LabelEn, startsAt = result.Entity.StartsAt, endsAt = result.Entity.EndsAt, updatedAt = result.Entity.UpdatedAt }),
        PhaseScheduleCommandStatus.NotFound => Results.NotFound(),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("SupervisorOrAdmin");

app.MapPost("/api/admin/phases/{idx:int}/announce", async (int idx, ClaimsPrincipal user, IPhaseScheduleService service) =>
{
    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.AnnounceAsync(idx, actorId);
    return result.Status switch
    {
        PhaseAnnounceStatus.Success => Results.Ok(new { recipientCount = result.RecipientCount }),
        PhaseAnnounceStatus.NotFound => Results.NotFound(),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("SupervisorOrAdmin");

app.MapGet("/api/admin/phases/{idx:int}/audience", async (int idx, IPhaseScheduleService service) =>
{
    var roleCodes = await service.GetAudienceAsync(idx);
    return Results.Ok(new { roleCodes });
}).RequireAuthorization("SupervisorOrAdmin");

app.MapPut("/api/admin/phases/{idx:int}/audience", async (int idx, PhaseAudienceInput input, IPhaseScheduleService service) =>
{
    await service.SetAudienceAsync(idx, input.RoleCodes);
    var roleCodes = await service.GetAudienceAsync(idx);
    return Results.Ok(new { roleCodes });
}).RequireAuthorization("SupervisorOrAdmin");

app.MapGet("/api/activities", async (InnovationDbContext db) =>
{
    var activities = await db.Activities
        .OrderBy(a => a.StartDate)
        .Select(a => new { id = a.Id, nameAr = a.NameAr, nameEn = a.NameEn })
        .ToListAsync();
    return Results.Ok(activities);
}).RequireAuthorization("AnyAssignedRole");

app.MapGet("/api/challenges", async (Guid themeId, IChallengeService service) =>
{
    var challenges = await service.ListActiveByThemeAsync(themeId);
    return Results.Ok(challenges.Select(c => new { id = c.Id, textAr = c.TextAr, textEn = c.TextEn }));
}).RequireAuthorization("AnyAssignedRole");

app.MapGet("/api/admin/challenges", async (IChallengeService service) =>
{
    var challenges = await service.ListAsync();
    return Results.Ok(challenges.Select(c => new { id = c.Id, strategicThemeId = c.StrategicThemeId, textAr = c.TextAr, textEn = c.TextEn, sortOrder = c.SortOrder, isActive = c.IsActive }));
}).RequireAuthorization("AdminOnly");

app.MapGet("/api/admin/challenges/{id:guid}", async (Guid id, IChallengeService service) =>
{
    var challenge = await service.GetByIdAsync(id);
    if (challenge is null) return Results.NotFound();
    return Results.Ok(new { id = challenge.Id, strategicThemeId = challenge.StrategicThemeId, textAr = challenge.TextAr, textEn = challenge.TextEn, sortOrder = challenge.SortOrder, isActive = challenge.IsActive });
}).RequireAuthorization("AdminOnly");

app.MapPost("/api/admin/challenges", async (ChallengeInput input, ClaimsPrincipal user, IChallengeService service) =>
{
    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.CreateAsync(input, actorId);
    return result.Status switch
    {
        ChallengeCommandStatus.Success => Results.Ok(new { id = result.Entity!.Id, strategicThemeId = result.Entity.StrategicThemeId, textAr = result.Entity.TextAr, textEn = result.Entity.TextEn, sortOrder = result.Entity.SortOrder, isActive = result.Entity.IsActive }),
        ChallengeCommandStatus.InvalidStrategicTheme => Results.BadRequest(new { error = "Strategic theme does not exist." }),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("AdminOnly");

app.MapPut("/api/admin/challenges/{id:guid}", async (Guid id, ChallengeInput input, ClaimsPrincipal user, IChallengeService service) =>
{
    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.UpdateAsync(id, input, actorId);
    return result.Status switch
    {
        ChallengeCommandStatus.Success => Results.Ok(new { id = result.Entity!.Id, strategicThemeId = result.Entity.StrategicThemeId, textAr = result.Entity.TextAr, textEn = result.Entity.TextEn, sortOrder = result.Entity.SortOrder, isActive = result.Entity.IsActive }),
        ChallengeCommandStatus.NotFound => Results.NotFound(),
        ChallengeCommandStatus.InvalidStrategicTheme => Results.BadRequest(new { error = "Strategic theme does not exist." }),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("AdminOnly");

app.MapDelete("/api/admin/challenges/{id:guid}", async (Guid id, ClaimsPrincipal user, IChallengeService service) =>
{
    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.DeleteAsync(id, actorId);
    return result.Status switch
    {
        ChallengeCommandStatus.Success => Results.NoContent(),
        ChallengeCommandStatus.NotFound => Results.NotFound(),
        ChallengeCommandStatus.InUse => Results.Conflict(new { error = "Cannot delete a challenge that ideas currently reference. Deactivate it instead by setting Active to false." }),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("AdminOnly");

app.MapPost("/api/ideas/{id:guid}/evaluations", async (Guid id, EvaluationInput input, ClaimsPrincipal user, IEvaluationService service, ISlaClockService slaClockService) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.SubmitAsync(id, userId, input);
    // Change 20260726 — a saved draft is not a completed evaluation, so its SLA clock keeps running.
    if (result.Status == EvaluationCommandStatus.Success && result.Evaluation!.SubmittedAt is not null)
    {
        await slaClockService.CloseAsync("evaluation", id);
    }
    return result.Status switch
    {
        EvaluationCommandStatus.Success => Results.Ok(new { id = result.Evaluation!.Id, totalScore = result.Evaluation.TotalScore, recommendation = result.Evaluation.Recommendation, conflictOfInterest = result.Evaluation.ConflictOfInterest, submittedAt = result.Evaluation.SubmittedAt, ideaStatus = result.Idea!.IdeaStatus.Code }), // Change 20260726
        EvaluationCommandStatus.NotFound => Results.NotFound(),
        EvaluationCommandStatus.Forbidden => Results.StatusCode(StatusCodes.Status403Forbidden),
        EvaluationCommandStatus.InvalidState => Results.BadRequest(new { error = "Idea is not awaiting evaluation." }),
        EvaluationCommandStatus.AlreadyEvaluated => Results.BadRequest(new { error = "You have already evaluated this idea." }),
        EvaluationCommandStatus.InvalidScore => Results.BadRequest(new { error = "Scores must be between 0 and 10." }),
        EvaluationCommandStatus.InvalidCriteria => Results.BadRequest(new { error = "Criteria scores must include exactly the active evaluation criteria." }), // Change 20260726
        EvaluationCommandStatus.SelfAuthorship => Results.StatusCode(StatusCodes.Status403Forbidden), // Change 20260726
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("EvaluatorAndAbove");

// Statuses reachable only once the supervisor has forwarded evaluation feedback onward
// (idea has left evaluation_review for submitter_review or beyond).
var forwardedEvaluationReviewStatuses = new HashSet<string>
{
    IdeaStatusCodes.SubmitterReview,
    IdeaStatusCodes.CommitteePending,
    IdeaStatusCodes.Committee,
    IdeaStatusCodes.PendingFinalRanking,
    IdeaStatusCodes.Approved,
    IdeaStatusCodes.NotSelected,
    IdeaStatusCodes.InPilot,
    IdeaStatusCodes.InMeasurement,
    IdeaStatusCodes.InScaling,
};

app.MapGet("/api/ideas/{id:guid}/evaluations", async (Guid id, ClaimsPrincipal user, InnovationDbContext db) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var idea = await db.Ideas.Include(i => i.IdeaStatus).SingleOrDefaultAsync(i => i.Id == id);
    if (idea is null || idea.SubmitterId != userId) return Results.NotFound();

    // The submitter must never see round-1 evaluator scores — only comments, and only once the
    // supervisor has forwarded feedback (idea has left evaluation_review onward to submitter_review
    // or beyond). Before that, the submitter hasn't been sent anything yet.
    if (!forwardedEvaluationReviewStatuses.Contains(idea.IdeaStatus.Code))
    {
        return Results.Ok(new { evaluations = Array.Empty<object>(), supervisorComment = (string?)null });
    }

    var submitted = await db.Evaluations
        .Where(e => e.IdeaId == id && e.SubmittedAt != null)
        .OrderBy(e => e.SubmittedAt)
        .ToListAsync();

    var evaluations = submitted.Select((e, i) => new
    {
        reviewerLabel = $"Reviewer {i + 1}",
        comment = e.Comments,
    }).ToList();

    return Results.Ok(new { evaluations, supervisorComment = idea.SupervisorEvaluationComment });
}).RequireAuthorization();

app.MapGet("/api/ideas/{id:guid}/journey", async (Guid id, ClaimsPrincipal user, InnovationDbContext db, IEvaluationSettingsService settings) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var isElevatedReviewer = user.IsInRole(RoleCodes.Evaluator) || user.IsInRole(RoleCodes.Judge) || user.IsInRole(RoleCodes.Supervisor) || user.IsInRole(RoleCodes.Admin);

    var idea = await db.Ideas.Include(i => i.IdeaStatus).SingleOrDefaultAsync(i => i.Id == id);
    if (idea is null) return Results.NotFound();
    if (idea.SubmitterId != userId && !isElevatedReviewer) return Results.StatusCode(StatusCodes.Status403Forbidden);

    var assignmentDates = await db.Assignments.Where(a => a.IdeaId == id).Select(a => (DateTime?)a.AssignedAt).ToListAsync();
    var evaluationRows = await db.Evaluations.Where(e => e.IdeaId == id && e.SubmittedAt != null)
        .Select(e => new { e.TotalScore, e.SubmittedAt }).ToListAsync();
    var decisionRows = await db.CommitteeDecisions.Where(c => c.IdeaId == id && c.DecidedAt != null)
        .Select(c => new { c.CommitteeDecisionType.Code, c.DecidedAt }).ToListAsync();

    // Recomputed against the CURRENT threshold for display only; the authoritative pass/fail was
    // persisted at submit time by EvaluationService. IdeaJourneyCalculator resolves terminal
    // statuses (pass_awaiting_attachments / evaluation_failed) via the status branch BEFORE the
    // recomputed score branches, so a later threshold change can't retroactively flip a decided idea.
    var passThreshold = await settings.GetPassThresholdAsync();

    var journey = IdeaJourneyCalculator.Compute(
        new JourneyIdeaInput(idea.IdeaStatus.Code, idea.EnteredEvaluationAt ?? idea.CreatedAt, idea.CreatedAt, idea.UpdatedAt),
        assignmentDates.Select(d => new JourneyAssignmentInput(d)).ToList(),
        evaluationRows.Select(e => new JourneyEvaluationInput((double?)e.TotalScore, e.SubmittedAt)).ToList(),
        decisionRows.Select(d => new JourneyCommitteeDecisionInput(
            d.Code.StartsWith("approv", StringComparison.OrdinalIgnoreCase) ? "approve"
                : d.Code.StartsWith("reject", StringComparison.OrdinalIgnoreCase) ? "reject"
                : d.Code,
            d.DecidedAt)).ToList(),
        (double)passThreshold);

    // The round-1 evaluator aggregate score must never reach the submitter — only supervisor/judge/admin
    // may see it (see the workflow confidentiality matrix). The submitter's journey shows stages only.
    var canSeeEvaluationScore = user.IsInRole(RoleCodes.Supervisor) || user.IsInRole(RoleCodes.Judge) || user.IsInRole(RoleCodes.Admin);

    return Results.Ok(new
    {
        currentStage = journey.CurrentStage,
        stopped = journey.Stopped,
        evaluationScore = canSeeEvaluationScore ? journey.EvaluationScore : (double?)null,
        stages = journey.Stages.Select(s => new
        {
            index = s.Index,
            state = s.State.ToString().ToLowerInvariant(),
            label = new { ar = s.Label.Ar, en = s.Label.En },
            completedAt = s.CompletedAt,
        }),
    });
}).RequireAuthorization("AnyAssignedRole");

app.MapGet("/api/evaluations/queue", async (ClaimsPrincipal user, IEvaluationService service) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var ideas = await service.GetQueueAsync(userId);
    return Results.Ok(ideas.Select(i => new
    {
        id = i.Id,
        code = i.Code,
        titleAr = i.TitleAr,
        titleEn = i.TitleEn,
        strategicThemeId = i.StrategicThemeId,
        updatedAt = i.UpdatedAt,
    }));
}).RequireAuthorization("EvaluatorAndAbove");

app.MapGet("/api/evaluations/mine", async (ClaimsPrincipal user, IEvaluationService service) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var evaluations = await service.GetMyEvaluationsAsync(userId);
    return Results.Ok(evaluations.Select(e => new
    {
        id = e.Id,
        ideaId = e.IdeaId,
        ideaCode = e.Idea.Code,
        ideaTitleEn = e.Idea.TitleAr,
        totalScore = e.TotalScore,
        recommendation = e.Recommendation,
        submittedAt = e.SubmittedAt,
        ideaEnteredEvaluationAt = e.Idea.EnteredEvaluationAt,
        // Change 20260726 — the form resumes a draft from this payload, so it carries the saved scores.
        criteriaScoresJson = e.CriteriaScoresJson,
        comments = e.Comments,
        conflictOfInterest = e.ConflictOfInterest,
    }));
}).RequireAuthorization("EvaluatorAndAbove");

app.MapPost("/api/ideas/{id:guid}/submit-to-committee", async (Guid id, SubmitToCommitteeInput input, ClaimsPrincipal user, ICommitteeReferralService service, ISlaClockService slaClockService) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.SubmitToCommitteeAsync(id, userId, input);
    if (result.Status == CommitteeReferralCommandStatus.Success)
    {
        await slaClockService.OpenAsync("committee", id);
    }
    return result.Status switch
    {
        CommitteeReferralCommandStatus.Success => Results.Ok(new { id = result.Idea!.Id, status = result.Idea.IdeaStatus.Code }),
        CommitteeReferralCommandStatus.NotFound => Results.NotFound(),
        CommitteeReferralCommandStatus.InvalidState => Results.BadRequest(new { error = "Idea must be in committee_pending state to submit to committee." }),
        CommitteeReferralCommandStatus.JudgesRequired => Results.BadRequest(new { error = "Select at least one judge." }),
        CommitteeReferralCommandStatus.InvalidJudge => Results.BadRequest(new { error = "One or more selected users are not judges." }),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("SupervisorOrAdmin");

app.MapPost("/api/ideas/{id:guid}/withdraw", async (Guid id, IdeaWithdrawInput? input, ClaimsPrincipal user, IIdeaService service) => // Change 20260726
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.WithdrawAsync(id, userId, input?.Reason); // Change 20260726
    return result.Status switch
    {
        IdeaCommandStatus.Success => Results.Ok(new { id = result.Idea!.Id, status = result.Idea.IdeaStatus.Code }),
        IdeaCommandStatus.NotFound => Results.NotFound(),
        IdeaCommandStatus.Forbidden => Results.Forbid(),
        _ => Results.Conflict(new { error = "Idea cannot be withdrawn in its current state." }),
    };
}).RequireAuthorization("IdeaAuthor");

// Change 20260726
// Change 20260726
// StoredPath is deliberately omitted — clients address attachments by id via the download endpoint.
static object[] CommitteeDecisionAttachmentResponse(CommitteeDecision decision) =>
    CommitteeDecisionAttachment.Parse(decision.AttachmentsJson)
        .Select(a => (object)new { id = a.Id, fileName = a.FileName, contentType = a.ContentType, sizeBytes = a.SizeBytes, uploadedAt = a.UploadedAt })
        .ToArray();

static object EvaluationCriterionResponse(EvaluationCriterion c) => new
{
    id = c.Id,
    code = c.Code,
    nameAr = c.NameAr,
    nameEn = c.NameEn,
    descriptionAr = c.DescriptionAr,
    descriptionEn = c.DescriptionEn,
    weight = c.Weight,
    active = c.Active,
    sortOrder = c.SortOrder,
};

// Change 20260726
app.MapGet("/api/evaluation-criteria", async (IEvaluationCriteriaService service) =>
{
    var criteria = await service.ListActiveAsync();
    return Results.Ok(criteria.Select(c => new
    {
        code = c.Code,
        nameAr = c.NameAr,
        nameEn = c.NameEn,
        descriptionAr = c.DescriptionAr,
        descriptionEn = c.DescriptionEn,
        weight = c.Weight,
        sortOrder = c.SortOrder,
    }));
}).RequireAuthorization("EvaluatorAndAbove");

// Change 20260726
app.MapGet("/api/admin/evaluation-criteria", async (IEvaluationCriteriaService service) =>
{
    var criteria = await service.ListAllAsync();
    return Results.Ok(criteria.Select(c => new
    {
        id = c.Id,
        code = c.Code,
        nameAr = c.NameAr,
        nameEn = c.NameEn,
        descriptionAr = c.DescriptionAr,
        descriptionEn = c.DescriptionEn,
        weight = c.Weight,
        active = c.Active,
        sortOrder = c.SortOrder,
    }));
}).RequireAuthorization("SupervisorOrAdmin");

// Change 20260726
app.MapPost("/api/admin/evaluation-criteria", async (EvaluationCriterionInput input, ClaimsPrincipal user, IEvaluationCriteriaService service) =>
{
    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.CreateAsync(input, actorId);
    return result.Status switch
    {
        EvaluationCriteriaCommandStatus.Success => Results.Ok(EvaluationCriterionResponse(result.Entity!)),
        EvaluationCriteriaCommandStatus.DuplicateCode => Results.BadRequest(new { error = "An evaluation criterion with this code already exists." }),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("SupervisorOrAdmin");

// Change 20260726
app.MapPut("/api/admin/evaluation-criteria/{id:guid}", async (Guid id, EvaluationCriterionInput input, ClaimsPrincipal user, IEvaluationCriteriaService service) =>
{
    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.UpdateAsync(id, input, actorId);
    return result.Status switch
    {
        EvaluationCriteriaCommandStatus.Success => Results.Ok(EvaluationCriterionResponse(result.Entity!)),
        EvaluationCriteriaCommandStatus.NotFound => Results.NotFound(),
        EvaluationCriteriaCommandStatus.DuplicateCode => Results.BadRequest(new { error = "An evaluation criterion with this code already exists." }),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("SupervisorOrAdmin");

// Change 20260726
app.MapDelete("/api/admin/evaluation-criteria/{id:guid}", async (Guid id, ClaimsPrincipal user, IEvaluationCriteriaService service) =>
{
    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.DeleteAsync(id, actorId);
    return result.Status switch
    {
        EvaluationCriteriaCommandStatus.Success => Results.NoContent(),
        EvaluationCriteriaCommandStatus.NotFound => Results.NotFound(),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("SupervisorOrAdmin");

app.MapGet("/api/committee-criteria", async (InnovationDbContext db) =>
{
    var criteria = await db.CommitteeCriteria
        .Where(c => c.Active)
        .Select(c => new { code = c.Code, nameAr = c.NameAr, nameEn = c.NameEn, weight = c.Weight })
        .ToListAsync();
    return Results.Ok(criteria);
}).RequireAuthorization("SupervisorOrCommittee");

app.MapGet("/api/admin/committee-criteria", async (ICommitteeCriteriaService service) =>
{
    var criteria = await service.ListAllAsync();
    return Results.Ok(criteria.Select(c => new
    {
        id = c.Id,
        code = c.Code,
        nameAr = c.NameAr,
        nameEn = c.NameEn,
        descriptionAr = c.DescriptionAr,
        descriptionEn = c.DescriptionEn,
        weight = c.Weight,
        active = c.Active,
    }));
}).RequireAuthorization("SupervisorOrAdmin");

app.MapPost("/api/admin/committee-criteria", async (CommitteeCriterionInput input, ClaimsPrincipal user, ICommitteeCriteriaService service) =>
{
    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.CreateAsync(input, actorId);
    return result.Status switch
    {
        CommitteeCriteriaCommandStatus.Success => Results.Ok(new
        {
            id = result.Entity!.Id,
            code = result.Entity.Code,
            nameAr = result.Entity.NameAr,
            nameEn = result.Entity.NameEn,
            descriptionAr = result.Entity.DescriptionAr,
            descriptionEn = result.Entity.DescriptionEn,
            weight = result.Entity.Weight,
            active = result.Entity.Active,
        }),
        CommitteeCriteriaCommandStatus.DuplicateCode => Results.BadRequest(new { error = "A committee criterion with this code already exists." }),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("SupervisorOrAdmin");

app.MapPut("/api/admin/committee-criteria/{id:guid}", async (Guid id, CommitteeCriterionInput input, ClaimsPrincipal user, ICommitteeCriteriaService service) =>
{
    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.UpdateAsync(id, input, actorId);
    return result.Status switch
    {
        CommitteeCriteriaCommandStatus.Success => Results.Ok(new
        {
            id = result.Entity!.Id,
            code = result.Entity.Code,
            nameAr = result.Entity.NameAr,
            nameEn = result.Entity.NameEn,
            descriptionAr = result.Entity.DescriptionAr,
            descriptionEn = result.Entity.DescriptionEn,
            weight = result.Entity.Weight,
            active = result.Entity.Active,
        }),
        CommitteeCriteriaCommandStatus.NotFound => Results.NotFound(),
        CommitteeCriteriaCommandStatus.DuplicateCode => Results.BadRequest(new { error = "A committee criterion with this code already exists." }),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("SupervisorOrAdmin");

app.MapDelete("/api/admin/committee-criteria/{id:guid}", async (Guid id, ClaimsPrincipal user, ICommitteeCriteriaService service) =>
{
    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.DeleteAsync(id, actorId);
    return result.Status switch
    {
        CommitteeCriteriaCommandStatus.Success => Results.NoContent(),
        CommitteeCriteriaCommandStatus.NotFound => Results.NotFound(),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("SupervisorOrAdmin");

// Change 20260726
// Multipart form-data so a judge can attach supporting files alongside the decision. Scores arrive
// as the `criteriaScores` JSON field because form fields are flat strings.
app.MapPost("/api/ideas/{id:guid}/committee-decisions", async (Guid id, HttpRequest request, ClaimsPrincipal user, ICommitteeService service, ISlaClockService slaClockService) =>
{
    if (!request.HasFormContentType) return Results.BadRequest(new { error = "Expected multipart form-data." });
    var form = await request.ReadFormAsync();

    var decisionTypeCode = form["decisionType"].ToString();
    if (string.IsNullOrWhiteSpace(decisionTypeCode)) return Results.BadRequest(new { error = "Invalid decision type." });

    Dictionary<string, decimal>? criteriaScores;
    try
    {
        criteriaScores = JsonSerializer.Deserialize<Dictionary<string, decimal>>(form["criteriaScores"].ToString());
    }
    catch (JsonException)
    {
        criteriaScores = null;
    }
    if (criteriaScores is null) return Results.BadRequest(new { error = "Criteria scores must include exactly the active criteria, each between 0 and 10." });

    var comments = form["comments"].ToString();

    var uploads = new List<CommitteeDecisionAttachmentUpload>();
    foreach (var file in form.Files.GetFiles("attachments"))
    {
        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        uploads.Add(new CommitteeDecisionAttachmentUpload(file.FileName, file.ContentType, stream.ToArray()));
    }

    var input = new CommitteeDecisionInput(
        decisionTypeCode,
        criteriaScores,
        string.IsNullOrWhiteSpace(comments) ? null : comments,
        uploads);

    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.SubmitDecisionAsync(id, userId, input);
    if (result.Status == CommitteeCommandStatus.Success && result.Idea!.IdeaStatus.Code == IdeaStatusCodes.PendingFinalRanking)
    {
        await slaClockService.CloseAsync("committee", id);
    }
    return result.Status switch
    {
        CommitteeCommandStatus.Success => Results.Ok(new
        {
            id = result.Decision!.Id,
            totalScore = result.Decision.TotalScore,
            ideaStatus = result.Idea!.IdeaStatus.Code,
            attachments = CommitteeDecisionAttachmentResponse(result.Decision), // Change 20260726
        }),
        CommitteeCommandStatus.NotFound => Results.NotFound(),
        CommitteeCommandStatus.InvalidState => Results.BadRequest(new { error = "Idea is not awaiting committee review." }),
        CommitteeCommandStatus.AlreadyDecided => Results.BadRequest(new { error = "You have already decided on this idea." }),
        CommitteeCommandStatus.InvalidDecisionType => Results.BadRequest(new { error = "Invalid decision type." }),
        CommitteeCommandStatus.InvalidCriteria => Results.BadRequest(new { error = "Criteria scores must include exactly the active criteria, each between 0 and 10." }),
        CommitteeCommandStatus.InvalidAttachment => Results.BadRequest(new { error = "Unsupported file type or file exceeds the size limit." }), // Change 20260726
        CommitteeCommandStatus.Forbidden => Results.StatusCode(StatusCodes.Status403Forbidden),
        CommitteeCommandStatus.SelfAuthorship => Results.StatusCode(StatusCodes.Status403Forbidden), // Change 20260726
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("SupervisorOrCommittee").DisableAntiforgery(); // Change 20260726

// Change 20260726
app.MapGet("/api/committee-decisions/{decisionId:guid}/attachments/{attachmentId:guid}", async (Guid decisionId, Guid attachmentId, ICommitteeService service) =>
{
    var result = await service.GetDecisionAttachmentFileAsync(decisionId, attachmentId);
    return result.Status switch
    {
        CommitteeCommandStatus.Success => Results.File(result.Content!, result.ContentType, result.FileName),
        CommitteeCommandStatus.NotFound => Results.NotFound(),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("SupervisorOrCommittee");

app.MapGet("/api/committee/queue", async (ClaimsPrincipal user, ICommitteeService service) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var queue = await service.GetQueueAsync(userId);
    return Results.Ok(queue.Select(q => new
    {
        id = q.Idea.Id,
        code = q.Idea.Code,
        titleAr = q.Idea.TitleAr,
        titleEn = q.Idea.TitleEn,
        submitterName = q.Idea.Submitter.FullNameAr,
        hasDecided = q.HasDecided,
        decidedCount = q.DecidedCount,
        totalJudges = q.TotalJudges,
        updatedAt = q.Idea.UpdatedAt,
    }));
}).RequireAuthorization("SupervisorOrCommittee");

app.MapGet("/api/committee/mine", async (ClaimsPrincipal user, ICommitteeService service) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var decisions = await service.GetMyDecisionsAsync(userId);
    return Results.Ok(decisions.Select(d => new
    {
        id = d.Id,
        ideaId = d.IdeaId,
        ideaCode = d.Idea.Code,
        ideaTitleEn = d.Idea.TitleAr,
        totalScore = d.TotalScore,
        decidedAt = d.DecidedAt,
        attachments = CommitteeDecisionAttachmentResponse(d), // Change 20260726
    }));
}).RequireAuthorization("SupervisorOrCommittee");

app.MapPost("/api/ideas/{id:guid}/screening-decision", async (Guid id, ScreeningDecisionInput input, ClaimsPrincipal user, IScreeningService service, ISlaClockService slaClockService) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.SubmitDecisionAsync(id, userId, input);
    if (result.Status == ScreeningCommandStatus.Success && result.Idea!.IdeaStatus.Code == IdeaStatusCodes.Evaluation)
    {
        await slaClockService.OpenAsync("evaluation", id);
    }
    return result.Status switch
    {
        ScreeningCommandStatus.Success => Results.Ok(new { id = result.Idea!.Id, status = result.Idea.IdeaStatus.Code }),
        ScreeningCommandStatus.NotFound => Results.NotFound(),
        ScreeningCommandStatus.InvalidState => Results.BadRequest(new { error = "Idea is not awaiting screening." }),
        ScreeningCommandStatus.ReasonRequired => Results.BadRequest(new { error = "A reason is required for this decision." }),
        ScreeningCommandStatus.InvalidDecision => Results.BadRequest(new { error = "Invalid screening decision." }),
        ScreeningCommandStatus.EvaluatorsRequired => Results.BadRequest(new { error = "Select at least one evaluator to approve." }),
        ScreeningCommandStatus.InvalidEvaluator => Results.BadRequest(new { error = "One or more selected users are not evaluators." }),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("SupervisorOrAdmin");

app.MapPost("/api/ideas/{id:guid}/evaluation-review-decision", async (Guid id, EvaluationReviewDecisionInput input, ClaimsPrincipal user, IEvaluationReviewService service) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.SubmitDecisionAsync(id, userId, input);
    return result.Status switch
    {
        EvaluationReviewCommandStatus.Success => Results.Ok(new { id = result.Idea!.Id, status = result.Idea.IdeaStatus.Code }),
        EvaluationReviewCommandStatus.NotFound => Results.NotFound(),
        EvaluationReviewCommandStatus.InvalidState => Results.BadRequest(new { error = "Idea is not awaiting evaluation review." }),
        EvaluationReviewCommandStatus.ReasonRequired => Results.BadRequest(new { error = "A reason is required for this decision." }),
        EvaluationReviewCommandStatus.InvalidDecision => Results.BadRequest(new { error = "Invalid evaluation-review decision." }),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("SupervisorOrAdmin");

app.MapPost("/api/ideas/{id:guid}/resubmit-evaluation", async (Guid id, ResubmitEvaluationInput input, ClaimsPrincipal user, ISubmitterReviewService service) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.ResubmitAsync(id, userId, input);
    return result.Status switch
    {
        SubmitterReviewCommandStatus.Success => Results.Ok(new { id = result.Idea!.Id, status = result.Idea.IdeaStatus.Code }),
        SubmitterReviewCommandStatus.NotFound => Results.NotFound(),
        SubmitterReviewCommandStatus.Forbidden => Results.StatusCode(StatusCodes.Status403Forbidden),
        SubmitterReviewCommandStatus.InvalidState => Results.BadRequest(new { error = "Idea is not awaiting your review." }),
        SubmitterReviewCommandStatus.CommentRequired => Results.BadRequest(new { error = "A comment is required." }),
        SubmitterReviewCommandStatus.BelowTarget => Results.BadRequest(new { error = "Idea did not meet the target rating." }),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("IdeaAuthor");

app.MapGet("/api/ideas/{id:guid}/evaluation-detail", async (Guid id, InnovationDbContext db) =>
{
    var idea = await db.Ideas.SingleOrDefaultAsync(i => i.Id == id);
    if (idea is null) return Results.NotFound();

    var submitted = await db.Evaluations
        .Where(e => e.IdeaId == id && e.SubmittedAt != null)
        .OrderBy(e => e.SubmittedAt)
        .ToListAsync();

    var evaluations = submitted.Select((e, i) => new
    {
        reviewerLabel = $"Reviewer {i + 1}",
        score = (double?)e.TotalScore,
        comment = e.Comments,
    }).ToList();

    Dictionary<string, decimal>? aggregateByCriteria = idea.EvaluationAggregateJson is null
        ? null
        : JsonSerializer.Deserialize<Dictionary<string, decimal>>(idea.EvaluationAggregateJson);

    return Results.Ok(new
    {
        evaluations,
        aggregateScore = idea.EvaluationAggregateScore,
        aggregateByCriteria,
        supervisorComment = idea.SupervisorEvaluationComment,
    });
}).RequireAuthorization("SupervisorOrCommittee");

app.MapGet("/api/screening/queue", async (IScreeningService service) =>
{
    var ideas = await service.GetQueueAsync();
    return Results.Ok(ideas.Select(i => new
    {
        id = i.Id,
        code = i.Code,
        titleAr = i.TitleAr,
        titleEn = i.TitleEn,
        submitterName = i.Submitter.FullNameAr,
        strategicThemeId = i.StrategicThemeId,
        updatedAt = i.UpdatedAt,
    }));
}).RequireAuthorization("SupervisorOrAdmin");

app.MapGet("/api/supervisor/evaluation-review-queue", async (IEvaluationReviewService service) =>
{
    var ideas = await service.GetReviewQueueAsync();
    return Results.Ok(ideas.Select(i => new
    {
        id = i.Id,
        code = i.Code,
        titleAr = i.TitleAr,
        titleEn = i.TitleEn,
        submitterName = i.Submitter.FullNameAr,
        strategicThemeId = i.StrategicThemeId,
        evaluationAggregateScore = i.EvaluationAggregateScore,
        updatedAt = i.UpdatedAt,
    }));
}).RequireAuthorization("SupervisorOrAdmin");

app.MapGet("/api/supervisor/committee-pending-queue", async (ICommitteeReferralService service) =>
{
    var ideas = await service.GetPendingQueueAsync();
    return Results.Ok(ideas.Select(i => new
    {
        id = i.Id,
        code = i.Code,
        titleAr = i.TitleAr,
        titleEn = i.TitleEn,
        submitterName = i.Submitter.FullNameAr,
        strategicThemeId = i.StrategicThemeId,
        evaluationAggregateScore = i.EvaluationAggregateScore,
        updatedAt = i.UpdatedAt,
    }));
}).RequireAuthorization("SupervisorOrAdmin");

app.MapGet("/api/track-assignments", async (ITrackAssignmentService service) =>
{
    var assignments = await service.ListAsync();
    return Results.Ok(assignments.Select(a => new
    {
        id = a.Id,
        evaluatorId = a.EvaluatorId,
        evaluatorName = a.Evaluator.FullNameAr,
        trackId = a.TrackId,
        trackNameEn = a.Track.NameAr,
    }));
}).RequireAuthorization("SupervisorOrAdmin");

app.MapPost("/api/track-assignments", async (TrackAssignmentRequest input, ClaimsPrincipal user, ITrackAssignmentService service) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.AssignAsync(input.EvaluatorId, input.TrackId, userId);
    return result.Status switch
    {
        TrackAssignmentCommandStatus.Success => Results.Created($"/api/track-assignments/{result.Assignment!.Id}", new { id = result.Assignment.Id }),
        TrackAssignmentCommandStatus.AlreadyAssigned => Results.BadRequest(new { error = "This evaluator is already assigned to this track." }),
        TrackAssignmentCommandStatus.InvalidEvaluator => Results.BadRequest(new { error = "The selected user does not hold the evaluator role." }),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("SupervisorOrAdmin");

app.MapDelete("/api/track-assignments/{id:guid}", async (Guid id, ITrackAssignmentService service) =>
{
    var result = await service.RemoveAsync(id);
    return result.Status switch
    {
        TrackAssignmentCommandStatus.Success => Results.NoContent(),
        TrackAssignmentCommandStatus.NotFound => Results.NotFound(),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("SupervisorOrAdmin");

app.MapGet("/api/users", async (string? role, InnovationDbContext db) =>
{
    var query = db.Users.AsQueryable();
    if (!string.IsNullOrEmpty(role))
    {
        query = query.Where(u => u.UserRoles.Any(ur => ur.Role.Code == role));
    }
    var users = await query
        .OrderBy(u => u.FullNameEn)
        .Select(u => new { id = u.Id, fullNameAr = u.FullNameAr, fullNameEn = u.FullNameEn })
        .ToListAsync();
    return Results.Ok(users);
}).RequireAuthorization("SupervisorOrAdmin");

app.MapGet("/api/final-ranking/preview", async (IFinalRankingService service) =>
{
    var result = await service.PreviewAsync();
    return Results.Ok(new
    {
        approvedCount = result.ApprovedCount,
        notSelectedCount = result.NotSelectedCount,
        topN = result.TopN,
        entries = result.Entries.Select(e => new { ideaId = e.IdeaId, code = e.Code, titleEn = e.TitleEn, trackId = e.TrackId, rank = e.Rank, score = e.Score, outcomeStatus = e.OutcomeStatus }),
    });
}).RequireAuthorization("SupervisorOrAdmin");

app.MapPost("/api/final-ranking/run", async (ClaimsPrincipal user, IFinalRankingService service) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.RunAsync(userId);
    return Results.Ok(new
    {
        approvedCount = result.ApprovedCount,
        notSelectedCount = result.NotSelectedCount,
        topN = result.TopN,
        entries = result.Entries.Select(e => new { ideaId = e.IdeaId, code = e.Code, titleEn = e.TitleEn, trackId = e.TrackId, rank = e.Rank, score = e.Score, outcomeStatus = e.OutcomeStatus }),
    });
}).RequireAuthorization("SupervisorOrAdmin");

app.MapGet("/api/approvals", async (ClaimsPrincipal user, IApprovalService svc) =>
{
    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
    var cards = await svc.GetPendingForUserAsync(userId, roles);
    return Results.Ok(new { items = cards.Select(c => new { instanceId = c.InstanceId, stepId = c.StepId, entityType = c.EntityType, entityId = c.EntityId, chainNameAr = c.ChainNameAr, chainNameEn = c.ChainNameEn, stepLabelAr = c.StepLabelAr, stepLabelEn = c.StepLabelEn, stepOrder = c.StepOrder, minApprovers = c.MinApprovers, priorApprovers = c.PriorApprovers }) });
}).RequireAuthorization("EvaluatorAndAbove");

app.MapPost("/api/approvals/decide", async (ApprovalDecideInput input, ClaimsPrincipal user, IApprovalService svc) =>
{
    if (input.Decision != "approve" && input.Decision != "reject")
    {
        return Results.BadRequest(new { error = "decision must be 'approve' or 'reject'." });
    }

    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
    var result = await svc.RecordDecisionAsync(input.InstanceId, input.StepId, userId, roles, input.Decision, input.Comment);
    return result.Status switch
    {
        ApprovalCommandStatus.Success => Results.Ok(new { instanceId = result.Instance!.Id, status = result.Instance.ApprovalInstanceStatus.Code }),
        ApprovalCommandStatus.NotFound => Results.NotFound(),
        ApprovalCommandStatus.Forbidden => Results.Forbid(),
        _ => Results.Conflict(new { error = "Decision cannot be recorded." }),
    };
}).RequireAuthorization("EvaluatorAndAbove");

app.MapPost("/api/approvals/bulk-decide", async (ApprovalBulkInput input, ClaimsPrincipal user, IApprovalService svc) =>
{
    if (input.Decision != "approve" && input.Decision != "reject")
    {
        return Results.BadRequest(new { error = "decision must be 'approve' or 'reject'." });
    }

    var userId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
    var (succeeded, failed) = await svc.BulkDecideAsync(input.Targets.Select(t => (t.InstanceId, t.StepId)).ToList(), userId, roles, input.Decision, input.Comment);
    return Results.Ok(new { succeeded, failed });
}).RequireAuthorization("EvaluatorAndAbove");

app.MapGet("/api/admin/users", async (IUserManagementService service) =>
{
    var users = await service.ListUsersAsync();
    return Results.Ok(users.Select(u => new
    {
        id = u.Id,
        samAccountName = u.SamAccountName,
        email = u.Email,
        fullNameAr = u.FullNameAr,
        fullNameEn = u.FullNameEn,
        department = u.Department,
        title = u.Title,
        isActive = u.IsActive,
        roles = u.UserRoles.Select(ur => new { roleId = ur.RoleId, code = ur.Role.Code, nameEn = ur.Role.NameAr }),
    }));
}).RequireAuthorization("AdminOnly");

app.MapGet("/api/admin/users/{id:guid}", async (Guid id, IUserManagementService service) =>
{
    var user = await service.GetUserDetailAsync(id);
    if (user is null) return Results.NotFound();
    return Results.Ok(new
    {
        id = user.Id,
        samAccountName = user.SamAccountName,
        email = user.Email,
        fullNameAr = user.FullNameAr,
        fullNameEn = user.FullNameEn,
        department = user.Department,
        title = user.Title,
        isActive = user.IsActive,
        roles = user.UserRoles.Select(ur => new { roleId = ur.RoleId, code = ur.Role.Code, nameEn = ur.Role.NameAr }),
    });
}).RequireAuthorization("AdminOnly");

app.MapPost("/api/admin/role-grants", async (RoleGrantInput input, ClaimsPrincipal user, IUserManagementService service) =>
{
    var granterId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.GrantRoleAsync(input, granterId);
    return result.Status switch
    {
        RoleGrantCommandStatus.GrantedImmediately => Results.Ok(new { status = "granted", userId = (Guid?)result.User!.Id, pendingGrantId = (Guid?)null }),
        RoleGrantCommandStatus.Pending => Results.Ok(new { status = "pending", userId = (Guid?)null, pendingGrantId = (Guid?)result.PendingGrant!.Id }),
        RoleGrantCommandStatus.RoleNotFound => Results.BadRequest(new { error = "Role does not exist." }),
        RoleGrantCommandStatus.AlreadyGranted => Results.BadRequest(new { error = "This user already has this role." }),
        RoleGrantCommandStatus.AlreadyPending => Results.BadRequest(new { error = "This role grant is already pending for this user." }),
        RoleGrantCommandStatus.AdUserNotFound => Results.BadRequest(new { error = "No matching Active Directory user was found for this account name." }),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("AdminOnly");

app.MapPost("/api/admin/directory/import", async (DirectoryImportRequest input, ClaimsPrincipal user, IUserManagementService service) =>
{
    if (input.SamAccountNames is null || input.SamAccountNames.Length == 0)
    {
        return Results.BadRequest(new { error = "At least one samAccountName is required." });
    }
    if (string.IsNullOrWhiteSpace(input.RoleCode))
    {
        return Results.BadRequest(new { error = "roleCode is required." });
    }

    var granterId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var results = new List<DirectoryImportResultItem>();
    foreach (var sam in input.SamAccountNames)
    {
        var result = await service.GrantRoleAsync(new RoleGrantInput(sam, input.RoleCode), granterId);
        results.Add(new DirectoryImportResultItem(sam, result.Status.ToString()));
    }

    return Results.Ok(results);
}).RequireAuthorization("AdminOnly");

// Bulk "import all users from AD": grants submitter to every directory user that currently has no
// roles (existing users) or queues a pending submitter grant (never-logged-in users). Idempotent.
app.MapPost("/api/admin/directory/import-all", async (ClaimsPrincipal user, IUserManagementService service) =>
{
    const int maxAdImportUsers = 5000;
    var granterId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.ImportAllAdUsersAsSubmittersAsync(granterId, maxAdImportUsers);
    return Results.Ok(new
    {
        total = result.Total,
        granted = result.Granted,
        pending = result.Pending,
        skippedHasRoles = result.SkippedHasRoles,
        skippedAlreadyPending = result.SkippedAlreadyPending,
    });
}).RequireAuthorization("AdminOnly");

app.MapPost("/api/admin/role-grants/group", async (GroupGrantRequest input, ClaimsPrincipal user, IUserManagementService service) =>
{
    var granterId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.GrantRoleToGroupAsync(input.GroupName, input.RoleCode, granterId);
    return Results.Ok(new
    {
        grantedCount = result.GrantedCount,
        pendingCount = result.PendingCount,
        skippedCount = result.SkippedCount,
        errors = result.Errors,
    });
}).RequireAuthorization("AdminOnly");

app.MapDelete("/api/admin/users/{userId:guid}/roles/{roleId:guid}", async (Guid userId, Guid roleId, IUserManagementService service) =>
{
    var result = await service.RevokeRoleAsync(userId, roleId);
    return result.Status switch
    {
        UserManagementCommandStatus.Success => Results.NoContent(),
        UserManagementCommandStatus.NotFound => Results.NotFound(),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("AdminOnly");

app.MapPost("/api/admin/users/{id:guid}/active", async (Guid id, SetActiveRequest input, IUserManagementService service) =>
{
    var result = await service.SetActiveAsync(id, input.IsActive);
    return result.Status switch
    {
        UserManagementCommandStatus.Success => Results.NoContent(),
        UserManagementCommandStatus.NotFound => Results.NotFound(),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("AdminOnly");

app.MapGet("/api/admin/pending-role-grants", async (IUserManagementService service) =>
{
    var grants = await service.ListPendingGrantsAsync();
    return Results.Ok(grants.Select(g => new
    {
        id = g.Id,
        samAccountName = g.SamAccountName,
        roleCode = g.Role.Code,
        roleNameEn = g.Role.NameAr,
        grantedByName = g.GrantedBy.FullNameAr,
        grantedAt = g.GrantedAt,
    }));
}).RequireAuthorization("AdminOnly");

app.MapDelete("/api/admin/pending-role-grants/{id:guid}", async (Guid id, IUserManagementService service) =>
{
    var result = await service.CancelPendingGrantAsync(id);
    return result.Status switch
    {
        UserManagementCommandStatus.Success => Results.NoContent(),
        UserManagementCommandStatus.NotFound => Results.NotFound(),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("AdminOnly");

app.MapGet("/api/admin/cms/blocks", async (ICmsService service) =>
{
    var blocks = await service.ListBlocksAsync();
    return Results.Ok(blocks.Select(b => new { id = b.Id, key = b.Key, contentAr = b.ContentAr, contentEn = b.ContentEn, isPublished = b.IsPublished, updatedAt = b.UpdatedAt }));
}).RequireAuthorization("SupervisorOrAdmin");

app.MapGet("/api/admin/cms/blocks/{id:guid}", async (Guid id, ICmsService service) =>
{
    var block = await service.GetBlockAsync(id);
    if (block is null) return Results.NotFound();
    return Results.Ok(new { id = block.Id, key = block.Key, contentAr = block.ContentAr, contentEn = block.ContentEn, isPublished = block.IsPublished, updatedAt = block.UpdatedAt });
}).RequireAuthorization("SupervisorOrAdmin");

app.MapPost("/api/admin/cms/blocks", async (CmsBlockInput input, ClaimsPrincipal user, ICmsService service) =>
{
    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.CreateBlockAsync(input, actorId);
    return result.Status switch
    {
        CmsCommandStatus.Success => Results.Ok(new { id = result.Entity!.Id, key = result.Entity.Key, contentAr = result.Entity.ContentAr, contentEn = result.Entity.ContentEn, isPublished = result.Entity.IsPublished, updatedAt = result.Entity.UpdatedAt }),
        CmsCommandStatus.DuplicateKey => Results.BadRequest(new { error = "A block with this key already exists." }),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("SupervisorOrAdmin");

app.MapPut("/api/admin/cms/blocks/{id:guid}", async (Guid id, CmsBlockInput input, ClaimsPrincipal user, ICmsService service) =>
{
    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.UpdateBlockAsync(id, input, actorId);
    return result.Status switch
    {
        CmsCommandStatus.Success => Results.Ok(new { id = result.Entity!.Id, key = result.Entity.Key, contentAr = result.Entity.ContentAr, contentEn = result.Entity.ContentEn, isPublished = result.Entity.IsPublished, updatedAt = result.Entity.UpdatedAt }),
        CmsCommandStatus.NotFound => Results.NotFound(),
        CmsCommandStatus.DuplicateKey => Results.BadRequest(new { error = "A block with this key already exists." }),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("SupervisorOrAdmin");

app.MapDelete("/api/admin/cms/blocks/{id:guid}", async (Guid id, ClaimsPrincipal user, ICmsService service) =>
{
    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.DeleteBlockAsync(id, actorId);
    return result.Status switch
    {
        CmsCommandStatus.Success => Results.NoContent(),
        CmsCommandStatus.NotFound => Results.NotFound(),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("SupervisorOrAdmin");

app.MapGet("/api/admin/cms/content", async (ICmsService service) =>
{
    var content = await service.ListContentAsync();
    return Results.Ok(content.Select(c => new { id = c.Id, slug = c.Slug, titleAr = c.TitleAr, titleEn = c.TitleEn, bodyAr = c.BodyAr, bodyEn = c.BodyEn, isPublished = c.IsPublished, publishedAt = c.PublishedAt, updatedAt = c.UpdatedAt }));
}).RequireAuthorization("SupervisorOrAdmin");

app.MapGet("/api/admin/cms/content/{id:guid}", async (Guid id, ICmsService service) =>
{
    var content = await service.GetContentAsync(id);
    if (content is null) return Results.NotFound();
    return Results.Ok(new { id = content.Id, slug = content.Slug, titleAr = content.TitleAr, titleEn = content.TitleEn, bodyAr = content.BodyAr, bodyEn = content.BodyEn, isPublished = content.IsPublished, publishedAt = content.PublishedAt, updatedAt = content.UpdatedAt });
}).RequireAuthorization("SupervisorOrAdmin");

app.MapPost("/api/admin/cms/content", async (CmsContentInput input, ClaimsPrincipal user, ICmsService service) =>
{
    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.CreateContentAsync(input, actorId);
    return result.Status switch
    {
        CmsCommandStatus.Success => Results.Ok(new { id = result.Entity!.Id, slug = result.Entity.Slug, titleAr = result.Entity.TitleAr, titleEn = result.Entity.TitleEn, bodyAr = result.Entity.BodyAr, bodyEn = result.Entity.BodyEn, isPublished = result.Entity.IsPublished, publishedAt = result.Entity.PublishedAt, updatedAt = result.Entity.UpdatedAt }),
        CmsCommandStatus.DuplicateKey => Results.BadRequest(new { error = "A content page with this slug already exists." }),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("SupervisorOrAdmin");

app.MapPut("/api/admin/cms/content/{id:guid}", async (Guid id, CmsContentInput input, ClaimsPrincipal user, ICmsService service) =>
{
    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.UpdateContentAsync(id, input, actorId);
    return result.Status switch
    {
        CmsCommandStatus.Success => Results.Ok(new { id = result.Entity!.Id, slug = result.Entity.Slug, titleAr = result.Entity.TitleAr, titleEn = result.Entity.TitleEn, bodyAr = result.Entity.BodyAr, bodyEn = result.Entity.BodyEn, isPublished = result.Entity.IsPublished, publishedAt = result.Entity.PublishedAt, updatedAt = result.Entity.UpdatedAt }),
        CmsCommandStatus.NotFound => Results.NotFound(),
        CmsCommandStatus.DuplicateKey => Results.BadRequest(new { error = "A content page with this slug already exists." }),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("SupervisorOrAdmin");

app.MapDelete("/api/admin/cms/content/{id:guid}", async (Guid id, ClaimsPrincipal user, ICmsService service) =>
{
    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.DeleteContentAsync(id, actorId);
    return result.Status switch
    {
        CmsCommandStatus.Success => Results.NoContent(),
        CmsCommandStatus.NotFound => Results.NotFound(),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("SupervisorOrAdmin");

app.MapGet("/api/admin/cms/strings", async (ICmsService service) =>
{
    var strings = await service.ListStringsAsync();
    return Results.Ok(strings.Select(s => new { id = s.Id, key = s.Key, valueAr = s.ValueAr, valueEn = s.ValueEn, updatedAt = s.UpdatedAt }));
}).RequireAuthorization("SupervisorOrAdmin");

app.MapGet("/api/admin/cms/strings/{id:guid}", async (Guid id, ICmsService service) =>
{
    var contentString = await service.GetStringAsync(id);
    if (contentString is null) return Results.NotFound();
    return Results.Ok(new { id = contentString.Id, key = contentString.Key, valueAr = contentString.ValueAr, valueEn = contentString.ValueEn, updatedAt = contentString.UpdatedAt });
}).RequireAuthorization("SupervisorOrAdmin");

app.MapPost("/api/admin/cms/strings", async (ContentStringInput input, ClaimsPrincipal user, ICmsService service) =>
{
    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.CreateStringAsync(input, actorId);
    return result.Status switch
    {
        CmsCommandStatus.Success => Results.Ok(new { id = result.Entity!.Id, key = result.Entity.Key, valueAr = result.Entity.ValueAr, valueEn = result.Entity.ValueEn, updatedAt = result.Entity.UpdatedAt }),
        CmsCommandStatus.DuplicateKey => Results.BadRequest(new { error = "A content string with this key already exists." }),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("SupervisorOrAdmin");

app.MapPut("/api/admin/cms/strings/{id:guid}", async (Guid id, ContentStringInput input, ClaimsPrincipal user, ICmsService service) =>
{
    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.UpdateStringAsync(id, input, actorId);
    return result.Status switch
    {
        CmsCommandStatus.Success => Results.Ok(new { id = result.Entity!.Id, key = result.Entity.Key, valueAr = result.Entity.ValueAr, valueEn = result.Entity.ValueEn, updatedAt = result.Entity.UpdatedAt }),
        CmsCommandStatus.NotFound => Results.NotFound(),
        CmsCommandStatus.DuplicateKey => Results.BadRequest(new { error = "A content string with this key already exists." }),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("SupervisorOrAdmin");

app.MapDelete("/api/admin/cms/strings/{id:guid}", async (Guid id, ClaimsPrincipal user, ICmsService service) =>
{
    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.DeleteStringAsync(id, actorId);
    return result.Status switch
    {
        CmsCommandStatus.Success => Results.NoContent(),
        CmsCommandStatus.NotFound => Results.NotFound(),
        _ => Results.StatusCode(StatusCodes.Status500InternalServerError),
    };
}).RequireAuthorization("SupervisorOrAdmin");

app.MapGet("/api/roles", async (InnovationDbContext db) =>
{
    var roles = await db.Roles
        .Where(r => r.IsActive)
        .OrderBy(r => r.SortOrder)
        .Select(r => new { id = r.Id, code = r.Code, nameEn = r.NameAr })
        .ToListAsync();
    return Results.Ok(roles);
}).RequireAuthorization("AdminOnly");

app.MapGet("/api/admin/roles", async (IRolesCatalogService service) =>
{
    var roles = await service.ListAllAsync();
    return Results.Ok(roles.Select(r => new
    {
        id = r.Id,
        code = r.Code,
        nameAr = r.NameAr,
        nameEn = r.NameEn,
        descriptionAr = r.DescriptionAr,
        descriptionEn = r.DescriptionEn,
        isSystem = r.IsSystem,
        isActive = r.IsActive,
        sortOrder = r.SortOrder,
    }));
}).RequireAuthorization("AdminOnly");

app.MapPatch("/api/admin/roles/{id:guid}", async (Guid id, RoleCatalogPatch input, ClaimsPrincipal user, IRolesCatalogService service) =>
{
    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var result = await service.PatchAsync(id, input, actorId);
    if (result.Status == RolePatchStatus.NotFound) return Results.NotFound();

    var r = result.Role!;
    return Results.Ok(new
    {
        id = r.Id,
        code = r.Code,
        nameAr = r.NameAr,
        nameEn = r.NameEn,
        descriptionAr = r.DescriptionAr,
        descriptionEn = r.DescriptionEn,
        isSystem = r.IsSystem,
        isActive = r.IsActive,
        sortOrder = r.SortOrder,
    });
}).RequireAuthorization("AdminOnly");

app.MapGet("/api/admin/report-titles", async (InnovationDbContext db) =>
{
    var titles = await db.ReportTitles
        .OrderBy(r => r.SortOrder)
        .ThenBy(r => r.TitleEn)
        .Select(r => new { id = r.Id, key = r.Key, titleAr = r.TitleAr, titleEn = r.TitleEn, sortOrder = r.SortOrder })
        .ToListAsync();
    return Results.Ok(titles);
}).RequireAuthorization("AdminOnly");

app.MapPost("/api/admin/report-titles", async (ReportTitleInput input, ClaimsPrincipal user, InnovationDbContext db, IAuditLogWriter auditLogWriter) =>
{
    var duplicate = await db.ReportTitles.AnyAsync(r => r.Key == input.Key);
    if (duplicate)
        return Results.BadRequest(new { error = "A report title with this key already exists." });

    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var title = new ReportTitle
    {
        Id = Guid.NewGuid(),
        Key = input.Key,
        TitleAr = input.TitleAr,
        TitleEn = input.TitleEn,
        SortOrder = input.SortOrder,
    };
    db.ReportTitles.Add(title);
    await db.SaveChangesAsync();

    await auditLogWriter.AppendAsync(
        "report_title",
        title.Id,
        "report_title.created",
        actorId,
        JsonSerializer.Serialize(input));

    return Results.Created($"/api/admin/report-titles/{title.Id}", new { id = title.Id });
}).RequireAuthorization("AdminOnly");

app.MapPut("/api/admin/report-titles/{id:guid}", async (Guid id, ReportTitlePatch input, ClaimsPrincipal user, InnovationDbContext db, IAuditLogWriter auditLogWriter) =>
{
    var title = await db.ReportTitles.SingleOrDefaultAsync(r => r.Id == id);
    if (title is null) return Results.NotFound();

    title.TitleAr = input.TitleAr;
    title.TitleEn = input.TitleEn;
    title.SortOrder = input.SortOrder;
    await db.SaveChangesAsync();

    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    await auditLogWriter.AppendAsync(
        "report_title",
        title.Id,
        "report_title.updated",
        actorId,
        JsonSerializer.Serialize(input));

    return Results.Ok(new { id = title.Id, key = title.Key, titleAr = title.TitleAr, titleEn = title.TitleEn, sortOrder = title.SortOrder });
}).RequireAuthorization("AdminOnly");

app.MapDelete("/api/admin/report-titles/{id:guid}", async (Guid id, ClaimsPrincipal user, InnovationDbContext db, IAuditLogWriter auditLogWriter) =>
{
    var title = await db.ReportTitles.SingleOrDefaultAsync(r => r.Id == id);
    if (title is null) return Results.NotFound();

    if (ProtectedReportTitleKeys.Contains(title.Key))
        return Results.BadRequest(new { error = "This report title is required by the platform and cannot be deleted." });

    db.ReportTitles.Remove(title);
    await db.SaveChangesAsync();

    var actorId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    await auditLogWriter.AppendAsync(
        "report_title",
        id,
        "report_title.deleted",
        actorId,
        null);

    return Results.NoContent();
}).RequireAuthorization("AdminOnly");

app.Run();

public partial class Program
{
    /// <summary>
    /// Keys of report titles that are system/seeded reference data and must not be deletable:
    /// the 12 report-type titles report generation resolves by <c>ReportTypeCodes</c> key, plus
    /// the 5 legacy export titles. Deleting one of these leaves report generation with a missing
    /// title (unhandled 500) or, if generations already reference it, an FK-restrict save failure.
    /// </summary>
    public static readonly HashSet<string> ProtectedReportTitleKeys = new(ReportTypeCodes.All)
    {
        "audit_log_export", "ideas_export", "evaluations_export", "escalations_export", "analytics_export",
    };
}

public sealed record HomePageSectionsReplaceRequest(List<HomePageSectionInput> Sections);

internal static class CallerIdentity
{
    /// <summary>
    /// Returns the caller's bare AD samAccountName for matching against stored IdeaTeamMember rows.
    /// Prefers the normalized <c>samAccountName</c> claim emitted by IdentityClaimsTransformation; if it's
    /// absent, falls back to <see cref="System.Security.Claims.ClaimsPrincipal.Identity"/>.Name with any
    /// <c>DOMAIN\</c> prefix or <c>@domain</c> UPN suffix stripped (Negotiate supplies a domain-qualified name).
    /// </summary>
    public static string? ResolveSam(ClaimsPrincipal user)
    {
        var claim = user.FindFirstValue(IdentityClaimsTransformation.SamAccountNameClaimType);
        if (!string.IsNullOrEmpty(claim)) return claim;

        var name = user.Identity?.Name;
        if (string.IsNullOrEmpty(name)) return name;

        var backslash = name.IndexOf('\\');
        if (backslash >= 0) name = name[(backslash + 1)..];
        var at = name.IndexOf('@');
        if (at >= 0) name = name[..at];
        return name;
    }
}

public sealed record DirectoryPersonDto(string SamAccountName, string DisplayName, string Email);

public sealed record DirectoryImportRequest(string[] SamAccountNames, string RoleCode);

public sealed record DirectoryImportResultItem(string SamAccountName, string Status);
