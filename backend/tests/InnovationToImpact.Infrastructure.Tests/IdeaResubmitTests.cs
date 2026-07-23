using InnovationToImpact.Domain.Audit;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Ideas;
using InnovationToImpact.Domain.Notifications;
using InnovationToImpact.Infrastructure.Data;
using InnovationToImpact.Infrastructure.Ideas;
using InnovationToImpact.Infrastructure.Screening;
using InnovationToImpact.Domain.Screening;
using InnovationToImpact.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class IdeaResubmitTests
{
    private sealed class FakeAuditLogWriter : IAuditLogWriter
    {
        public Task<AuditLog> AppendAsync(string entityType, Guid entityId, string action, Guid? actorId, string? payload, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AuditLog { Id = Guid.NewGuid(), EntityType = entityType, EntityId = entityId, Action = action, ActorId = actorId, Payload = payload });
        }
    }

    private sealed class FakeNotificationService : INotificationService
    {
        public Task<Notification> CreateAndPublishAsync(Guid userId, string notificationType, string titleAr, string titleEn, string bodyAr, string bodyEn, string? link, string? payloadJson, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new Notification { Id = Guid.NewGuid(), UserId = userId, NotificationType = notificationType, TitleAr = titleAr, TitleEn = titleEn, BodyAr = bodyAr, BodyEn = bodyEn });
        }
    }

    private static IdeaService MakeIdeaService(InnovationDbContext db, IEvidenceFileStorage storage) =>
        new(db, storage, new FakeAuditLogWriter(), new FakeNotificationService());

    private static Guid SeedUser(SqliteContextFixture fixture, string samAccountName)
    {
        using var db = fixture.CreateContext();
        var id = Guid.NewGuid();
        db.Users.Add(new User { Id = id, SamAccountName = samAccountName, Email = $"{samAccountName}@gac-demo.sa", FullNameAr = samAccountName, FullNameEn = samAccountName });
        db.SaveChanges();
        return id;
    }

    private static Guid SeedActivity(SqliteContextFixture fixture, Guid creatorId)
    {
        using var db = fixture.CreateContext();
        var id = Guid.NewGuid();
        db.Activities.Add(new Activity { Id = id, NameAr = "ف", NameEn = "Activity", Type = "hackathon", Status = "open", StartDate = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(30), CreatedById = creatorId });
        db.SaveChanges();
        return id;
    }

    private static (LocalDiskFileStorage Storage, string RootPath) MakeStorage()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), $"idea-resubmit-test-{Guid.NewGuid():N}");
        return (new LocalDiskFileStorage(rootPath), rootPath);
    }

    private static async Task<Idea> CreateReturnedIdeaAsync(SqliteContextFixture fixture, Guid submitterId, Guid activityId, Guid themeId, string? editableSections)
    {
        var (storage, rootPath) = MakeStorage();
        try
        {
            using var db = fixture.CreateContext();
            var ideaService = MakeIdeaService(db, storage);
            var input = new IdeaInput("ا", "T", "م", "P", "ح", "S", "ف", "B", themeId, activityId, null, "individual", null, Array.Empty<TeamMemberInput>(), true, true);
            var created = await ideaService.CreateAsync(submitterId, input);
            await ideaService.AddAttachmentAsync(created.Idea!.Id, submitterId, "a.pdf", "application/pdf", new byte[] { 1 });
            await ideaService.SubmitAsync(created.Idea.Id, submitterId);

            var returnedStatus = db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.Returned);
            var idea = db.Ideas.Single(i => i.Id == created.Idea.Id);
            idea.IdeaStatusId = returnedStatus.Id;
            idea.IdeaStatus = returnedStatus;
            idea.EditableSections = editableSections;
            db.SaveChanges();
            return idea;
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }

    private static IdeaResubmitInput MakeResubmitInput(Idea idea, string titleEn = "T") => new(
        TitleAr: idea.TitleAr,
        TitleEn: titleEn,
        ProposedSolutionAr: idea.ProposedSolutionAr,
        ProposedSolutionEn: idea.ProposedSolutionEn,
        ActivityId: idea.ActivityId!.Value,
        StrategicThemeId: idea.StrategicThemeId,
        ChallengeId: idea.ChallengeId,
        ParticipationType: idea.ParticipationType,
        TeamName: idea.TeamName,
        TeamMembers: Array.Empty<TeamMemberInput>());

    [Fact]
    public async Task ResubmitAsync_TitleSectionEditable_AppliesChangeAndTransitionsToSubmitted()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedUser(fixture, "submitter1");
        var activityId = SeedActivity(fixture, submitterId);
        using var themeDb = fixture.CreateContext();
        var themeId = themeDb.StrategicThemes.First().Id;
        var idea = await CreateReturnedIdeaAsync(fixture, submitterId, activityId, themeId, "title");

        using var db = fixture.CreateContext();
        var service = MakeIdeaService(db, new LocalDiskFileStorage(Path.GetTempPath()));
        var result = await service.ResubmitAsync(idea.Id, submitterId, MakeResubmitInput(idea, titleEn: "Updated Title"));

        Assert.Equal(IdeaCommandStatus.Success, result.Status);
        Assert.Equal("Updated Title", result.Idea!.TitleEn);
        Assert.Equal(IdeaStatusCodes.Submitted, result.Idea.IdeaStatus.Code);
        Assert.Null(result.Idea.EditableSections);
    }

    [Fact]
    public async Task ResubmitAsync_TitleSectionLocked_ChangingTitleReturnsSectionNotEditable()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedUser(fixture, "submitter1");
        var activityId = SeedActivity(fixture, submitterId);
        using var themeDb = fixture.CreateContext();
        var themeId = themeDb.StrategicThemes.First().Id;
        var idea = await CreateReturnedIdeaAsync(fixture, submitterId, activityId, themeId, "attachments");

        using var db = fixture.CreateContext();
        var service = MakeIdeaService(db, new LocalDiskFileStorage(Path.GetTempPath()));
        var result = await service.ResubmitAsync(idea.Id, submitterId, MakeResubmitInput(idea, titleEn: "Attempted Change"));

        Assert.Equal(IdeaCommandStatus.SectionNotEditable, result.Status);
    }

    [Fact]
    public async Task ResubmitAsync_NullEditableSections_AllowsAnySectionChange()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedUser(fixture, "submitter1");
        var activityId = SeedActivity(fixture, submitterId);
        using var themeDb = fixture.CreateContext();
        var themeId = themeDb.StrategicThemes.First().Id;
        var idea = await CreateReturnedIdeaAsync(fixture, submitterId, activityId, themeId, null);

        using var db = fixture.CreateContext();
        var service = MakeIdeaService(db, new LocalDiskFileStorage(Path.GetTempPath()));
        var result = await service.ResubmitAsync(idea.Id, submitterId, MakeResubmitInput(idea, titleEn: "Updated Title"));

        Assert.Equal(IdeaCommandStatus.Success, result.Status);
    }

    [Fact]
    public async Task ResubmitAsync_TeamSectionEditable_ReplacesRoster()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedUser(fixture, "submitter1");
        var activityId = SeedActivity(fixture, submitterId);
        using var themeDb = fixture.CreateContext();
        var themeId = themeDb.StrategicThemes.First().Id;

        var (storage, rootPath) = MakeStorage();
        Idea idea;
        try
        {
            using var db = fixture.CreateContext();
            var ideaService = MakeIdeaService(db, storage);
            var initialMembers = new[] { new TeamMemberInput("Original One", "o1@example.com"), new TeamMemberInput("Original Two", "o2@example.com") };
            var input = new IdeaInput("ا", "T", "م", "P", "ح", "S", "ف", "B", themeId, activityId, null, "team", "Original Team", initialMembers, true, true);
            var created = await ideaService.CreateAsync(submitterId, input);
            await ideaService.AddAttachmentAsync(created.Idea!.Id, submitterId, "a.pdf", "application/pdf", new byte[] { 1 });
            await ideaService.SubmitAsync(created.Idea.Id, submitterId);

            var returnedStatus = db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.Returned);
            var reloaded = db.Ideas.Single(i => i.Id == created.Idea.Id);
            reloaded.IdeaStatusId = returnedStatus.Id;
            reloaded.IdeaStatus = returnedStatus;
            reloaded.EditableSections = "team";
            db.SaveChanges();
            idea = reloaded;
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }

        using var resubmitDb = fixture.CreateContext();
        var service = MakeIdeaService(resubmitDb, new LocalDiskFileStorage(Path.GetTempPath()));
        var newMembers = new[] { new TeamMemberInput("Member One", "m1@example.com"), new TeamMemberInput("Member Two", "m2@example.com") };
        var input2 = MakeResubmitInput(idea) with { TeamMembers = newMembers };
        var result = await service.ResubmitAsync(idea.Id, submitterId, input2);

        Assert.Equal(IdeaCommandStatus.Success, result.Status);
        using var readDb = fixture.CreateContext();
        var savedMembers = readDb.IdeaTeamMembers.Where(m => m.IdeaId == idea.Id).ToList();
        Assert.Equal(2, savedMembers.Count);
        Assert.Contains(savedMembers, m => m.Name == "Member One");
    }

    [Fact]
    public async Task ResubmitAsync_TeamSectionEditableOnly_ChangingParticipationTypeReturnsSectionNotEditable()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedUser(fixture, "submitter1");
        var activityId = SeedActivity(fixture, submitterId);
        using var themeDb = fixture.CreateContext();
        var themeId = themeDb.StrategicThemes.First().Id;
        var idea = await CreateReturnedIdeaAsync(fixture, submitterId, activityId, themeId, "team");

        using var db = fixture.CreateContext();
        var service = MakeIdeaService(db, new LocalDiskFileStorage(Path.GetTempPath()));
        var members = new[] { new TeamMemberInput("Member One", "m1@example.com"), new TeamMemberInput("Member Two", "m2@example.com") };
        var input = MakeResubmitInput(idea) with { ParticipationType = "team", TeamName = "New Team", TeamMembers = members };
        var result = await service.ResubmitAsync(idea.Id, submitterId, input);

        Assert.Equal(IdeaCommandStatus.SectionNotEditable, result.Status);
    }

    [Fact]
    public async Task ResubmitAsync_ParticipationTypeSectionEditableOnly_ChangingTeamNameReturnsSectionNotEditable()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedUser(fixture, "submitter1");
        var activityId = SeedActivity(fixture, submitterId);
        using var themeDb = fixture.CreateContext();
        var themeId = themeDb.StrategicThemes.First().Id;
        var idea = await CreateReturnedIdeaAsync(fixture, submitterId, activityId, themeId, "participation_type");

        using var db = fixture.CreateContext();
        var service = MakeIdeaService(db, new LocalDiskFileStorage(Path.GetTempPath()));
        var input = MakeResubmitInput(idea) with { TeamName = "Sneaked In Team Name" };
        var result = await service.ResubmitAsync(idea.Id, submitterId, input);

        Assert.Equal(IdeaCommandStatus.SectionNotEditable, result.Status);
    }

    [Fact]
    public async Task ResubmitAsync_IdeaNotInReturnedState_ReturnsInvalidState()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedUser(fixture, "submitter1");
        var activityId = SeedActivity(fixture, submitterId);
        using var themeDb = fixture.CreateContext();
        var themeId = themeDb.StrategicThemes.First().Id;
        var (storage, rootPath) = MakeStorage();
        try
        {
            using var db = fixture.CreateContext();
            var ideaService = MakeIdeaService(db, storage);
            var input = new IdeaInput("ا", "T", "م", "P", "ح", "S", "ف", "B", themeId, activityId, null, "individual", null, Array.Empty<TeamMemberInput>(), true, true);
            var created = await ideaService.CreateAsync(submitterId, input);

            var result = await ideaService.ResubmitAsync(created.Idea!.Id, submitterId, MakeResubmitInput(created.Idea, "X"));

            Assert.Equal(IdeaCommandStatus.InvalidState, result.Status);
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }
}
