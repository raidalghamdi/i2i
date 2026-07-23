using InnovationToImpact.Domain.Audit;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Ideas;
using InnovationToImpact.Domain.Notifications;
using InnovationToImpact.Infrastructure.Data;
using InnovationToImpact.Infrastructure.Ideas;
using InnovationToImpact.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class IdeaServiceTests
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

    private static IdeaService MakeService(InnovationDbContext db, IEvidenceFileStorage storage) =>
        new(db, storage, new FakeAuditLogWriter(), new FakeNotificationService());

    private static Guid SeedSubmitter(SqliteContextFixture fixture, string samAccountName)
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
        db.Activities.Add(new Activity
        {
            Id = id,
            NameAr = "فعالية",
            NameEn = "Activity",
            Type = "hackathon",
            Status = "open",
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddDays(30),
            CreatedById = creatorId,
        });
        db.SaveChanges();
        return id;
    }

    private static IdeaInput MakeInput(Guid strategicThemeId, Guid activityId, Guid? challengeId = null, string participationType = "individual", string? teamName = null, IReadOnlyList<TeamMemberInput>? teamMembers = null, bool ipAcknowledged = true, bool termsAgreed = true) => new(
        TitleAr: "فكرة تجريبية",
        TitleEn: "Test Idea",
        ProblemStatementAr: "مشكلة",
        ProblemStatementEn: "Problem",
        ProposedSolutionAr: "حل",
        ProposedSolutionEn: "Solution",
        ExpectedBenefitsAr: "فوائد",
        ExpectedBenefitsEn: "Benefits",
        StrategicThemeId: strategicThemeId,
        ActivityId: activityId,
        ChallengeId: challengeId,
        ParticipationType: participationType,
        TeamName: teamName,
        TeamMembers: teamMembers ?? Array.Empty<TeamMemberInput>(),
        IpAcknowledged: ipAcknowledged,
        TermsAgreed: termsAgreed);

    private static (LocalDiskFileStorage Storage, string RootPath) MakeStorage()
    {
        var rootPath = Path.Combine(Path.GetTempPath(), $"idea-service-test-{Guid.NewGuid():N}");
        return (new LocalDiskFileStorage(rootPath), rootPath);
    }

    [Fact]
    public async Task CreateAsync_CreatesDraftIdeaWithSequentialCode()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedSubmitter(fixture, "submitter1");
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;
        var activityId = SeedActivity(fixture, submitterId);
        var (storage, rootPath) = MakeStorage();
        try
        {
            var service = MakeService(db, storage);

            var result = await service.CreateAsync(submitterId, MakeInput(themeId, activityId));

            Assert.Equal(IdeaCommandStatus.Success, result.Status);
            Assert.Equal("IDEA-0001", result.Idea!.Code);
            Assert.Equal(0, result.Idea.CurrentStage);
            Assert.Equal(submitterId, result.Idea.SubmitterId);
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task CreateAsync_SecondIdea_GetsNextSequentialCode()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedSubmitter(fixture, "submitter1");
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;
        var activityId = SeedActivity(fixture, submitterId);
        var (storage, rootPath) = MakeStorage();
        try
        {
            var service = MakeService(db, storage);
            await service.CreateAsync(submitterId, MakeInput(themeId, activityId));

            var second = await service.CreateAsync(submitterId, MakeInput(themeId, activityId));

            Assert.Equal("IDEA-0002", second.Idea!.Code);
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task CreateAsync_WithNonexistentStrategicTheme_ReturnsInvalidStrategicTheme()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedSubmitter(fixture, "submitter1");
        var activityId = SeedActivity(fixture, submitterId);
        using var db = fixture.CreateContext();
        var (storage, rootPath) = MakeStorage();
        try
        {
            var service = MakeService(db, storage);

            var result = await service.CreateAsync(submitterId, MakeInput(Guid.NewGuid(), activityId));

            Assert.Equal(IdeaCommandStatus.InvalidStrategicTheme, result.Status);
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task UpdateAsync_WithNonexistentStrategicTheme_ReturnsInvalidStrategicTheme()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedSubmitter(fixture, "submitter1");
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;
        var activityId = SeedActivity(fixture, submitterId);
        var (storage, rootPath) = MakeStorage();
        try
        {
            var service = MakeService(db, storage);
            var created = await service.CreateAsync(submitterId, MakeInput(themeId, activityId));

            var result = await service.UpdateAsync(created.Idea!.Id, submitterId, MakeInput(Guid.NewGuid(), activityId));

            Assert.Equal(IdeaCommandStatus.InvalidStrategicTheme, result.Status);
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task UpdateAsync_OnDraftByOwner_UpdatesFields()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedSubmitter(fixture, "submitter1");
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;
        var activityId = SeedActivity(fixture, submitterId);
        var (storage, rootPath) = MakeStorage();
        try
        {
            var service = MakeService(db, storage);
            var created = await service.CreateAsync(submitterId, MakeInput(themeId, activityId));

            var updated = await service.UpdateAsync(created.Idea!.Id, submitterId, MakeInput(themeId, activityId) with { TitleEn = "Updated Title" });

            Assert.Equal(IdeaCommandStatus.Success, updated.Status);
            Assert.Equal("Updated Title", updated.Idea!.TitleEn);
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task UpdateAsync_WrongOwner_ReturnsForbidden()
    {
        using var fixture = new SqliteContextFixture();
        var ownerId = SeedSubmitter(fixture, "submitter1");
        var otherId = SeedSubmitter(fixture, "submitter2");
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;
        var activityId = SeedActivity(fixture, ownerId);
        var (storage, rootPath) = MakeStorage();
        try
        {
            var service = MakeService(db, storage);
            var created = await service.CreateAsync(ownerId, MakeInput(themeId, activityId));

            var result = await service.UpdateAsync(created.Idea!.Id, otherId, MakeInput(themeId, activityId));

            Assert.Equal(IdeaCommandStatus.Forbidden, result.Status);
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task UpdateAsync_NonDraft_ReturnsInvalidState()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedSubmitter(fixture, "submitter1");
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;
        var activityId = SeedActivity(fixture, submitterId);
        var (storage, rootPath) = MakeStorage();
        try
        {
            var service = MakeService(db, storage);
            var created = await service.CreateAsync(submitterId, MakeInput(themeId, activityId));
            await service.AddAttachmentAsync(created.Idea!.Id, submitterId, "a.pdf", "application/pdf", new byte[] { 1 });
            await service.SubmitAsync(created.Idea.Id, submitterId);

            var result = await service.UpdateAsync(created.Idea.Id, submitterId, MakeInput(themeId, activityId));

            Assert.Equal(IdeaCommandStatus.InvalidState, result.Status);
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task SubmitAsync_WithZeroAttachments_ReturnsInvalidState()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedSubmitter(fixture, "submitter1");
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;
        var activityId = SeedActivity(fixture, submitterId);
        var (storage, rootPath) = MakeStorage();
        try
        {
            var service = MakeService(db, storage);
            var created = await service.CreateAsync(submitterId, MakeInput(themeId, activityId));

            var result = await service.SubmitAsync(created.Idea!.Id, submitterId);

            Assert.Equal(IdeaCommandStatus.InvalidState, result.Status);
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task SubmitAsync_WithAtLeastOneAttachment_TransitionsToSubmitted()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedSubmitter(fixture, "submitter1");
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;
        var activityId = SeedActivity(fixture, submitterId);
        var (storage, rootPath) = MakeStorage();
        try
        {
            var service = MakeService(db, storage);
            var created = await service.CreateAsync(submitterId, MakeInput(themeId, activityId));
            await service.AddAttachmentAsync(created.Idea!.Id, submitterId, "a.pdf", "application/pdf", new byte[] { 1, 2, 3 });

            var result = await service.SubmitAsync(created.Idea.Id, submitterId);

            Assert.Equal(IdeaCommandStatus.Success, result.Status);
            Assert.Equal(1, result.Idea!.CurrentStage);

            using var verifyDb = fixture.CreateContext();
            var reloaded = verifyDb.Ideas.Include(i => i.IdeaStatus).Single(i => i.Id == created.Idea.Id);
            Assert.Equal(IdeaStatusCodes.Submitted, reloaded.IdeaStatus.Code);
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task SubmitAsync_WithEvaluatorAssignedToTheme_StillTransitionsToSubmitted()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedSubmitter(fixture, "submitter1");
        var evaluatorId = SeedSubmitter(fixture, "evaluator1");
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;
        db.Set<EvaluatorTrackAssignment>().Add(new EvaluatorTrackAssignment { Id = Guid.NewGuid(), EvaluatorId = evaluatorId, TrackId = themeId, AssignedById = evaluatorId });
        db.SaveChanges();
        var activityId = SeedActivity(fixture, submitterId);

        var (storage, rootPath) = MakeStorage();
        try
        {
            var service = MakeService(db, storage);
            var created = await service.CreateAsync(submitterId, MakeInput(themeId, activityId));
            await service.AddAttachmentAsync(created.Idea!.Id, submitterId, "a.pdf", "application/pdf", new byte[] { 1 });

            var result = await service.SubmitAsync(created.Idea.Id, submitterId);

            Assert.Equal(IdeaCommandStatus.Success, result.Status);
            Assert.Equal(IdeaStatusCodes.Submitted, result.Idea!.IdeaStatus.Code);
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task UpdateAsync_OnReturnedByOwner_UpdatesFields()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedSubmitter(fixture, "submitter1");
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;
        var activityId = SeedActivity(fixture, submitterId);
        var (storage, rootPath) = MakeStorage();
        try
        {
            var service = MakeService(db, storage);
            var created = await service.CreateAsync(submitterId, MakeInput(themeId, activityId));

            var returnedStatus = db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.Returned);
            var idea = db.Ideas.Single(i => i.Id == created.Idea!.Id);
            idea.IdeaStatusId = returnedStatus.Id;
            db.SaveChanges();

            var updated = await service.UpdateAsync(created.Idea!.Id, submitterId, MakeInput(themeId, activityId) with { TitleEn = "Revised Title" });

            Assert.Equal(IdeaCommandStatus.Success, updated.Status);
            Assert.Equal("Revised Title", updated.Idea!.TitleEn);
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task SubmitAsync_FromReturned_TransitionsToSubmitted()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedSubmitter(fixture, "submitter1");
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;
        var activityId = SeedActivity(fixture, submitterId);
        var (storage, rootPath) = MakeStorage();
        try
        {
            var service = MakeService(db, storage);
            var created = await service.CreateAsync(submitterId, MakeInput(themeId, activityId));
            await service.AddAttachmentAsync(created.Idea!.Id, submitterId, "a.pdf", "application/pdf", new byte[] { 1 });

            var returnedStatus = db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.Returned);
            var idea = db.Ideas.Single(i => i.Id == created.Idea.Id);
            idea.IdeaStatusId = returnedStatus.Id;
            db.SaveChanges();

            var result = await service.SubmitAsync(created.Idea.Id, submitterId);

            Assert.Equal(IdeaCommandStatus.Success, result.Status);
            Assert.Equal(IdeaStatusCodes.Submitted, result.Idea!.IdeaStatus.Code);
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task UpdateAsync_OnRejected_ReturnsInvalidState()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedSubmitter(fixture, "submitter1");
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;
        var activityId = SeedActivity(fixture, submitterId);
        var (storage, rootPath) = MakeStorage();
        try
        {
            var service = MakeService(db, storage);
            var created = await service.CreateAsync(submitterId, MakeInput(themeId, activityId));

            var rejectedStatus = db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.Rejected);
            var idea = db.Ideas.Single(i => i.Id == created.Idea!.Id);
            idea.IdeaStatusId = rejectedStatus.Id;
            db.SaveChanges();

            var result = await service.UpdateAsync(created.Idea!.Id, submitterId, MakeInput(themeId, activityId));

            Assert.Equal(IdeaCommandStatus.InvalidState, result.Status);
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task GetByIdAsync_WrongOwner_ReturnsForbidden()
    {
        using var fixture = new SqliteContextFixture();
        var ownerId = SeedSubmitter(fixture, "submitter1");
        var otherId = SeedSubmitter(fixture, "submitter2");
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;
        var activityId = SeedActivity(fixture, ownerId);
        var (storage, rootPath) = MakeStorage();
        try
        {
            var service = MakeService(db, storage);
            var created = await service.CreateAsync(ownerId, MakeInput(themeId, activityId));

            var result = await service.GetByIdAsync(created.Idea!.Id, otherId);

            Assert.Equal(IdeaCommandStatus.Forbidden, result.Status);
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task GetByIdAsync_WrongOwnerButElevatedReviewer_ReturnsSuccess()
    {
        using var fixture = new SqliteContextFixture();
        var ownerId = SeedSubmitter(fixture, "submitter1");
        var reviewerId = SeedSubmitter(fixture, "evaluator1");
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;
        var activityId = SeedActivity(fixture, ownerId);
        var (storage, rootPath) = MakeStorage();
        try
        {
            var service = MakeService(db, storage);
            var created = await service.CreateAsync(ownerId, MakeInput(themeId, activityId));

            var result = await service.GetByIdAsync(created.Idea!.Id, reviewerId, isElevatedReviewer: true);

            Assert.Equal(IdeaCommandStatus.Success, result.Status);
            Assert.Equal(created.Idea.Id, result.Idea!.Id);
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNotFound()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedSubmitter(fixture, "submitter1");
        using var db = fixture.CreateContext();
        var (storage, rootPath) = MakeStorage();
        try
        {
            var service = MakeService(db, storage);

            var result = await service.GetByIdAsync(Guid.NewGuid(), submitterId);

            Assert.Equal(IdeaCommandStatus.NotFound, result.Status);
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task AddAttachmentAsync_DisallowedContentType_ReturnsInvalidAttachment()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedSubmitter(fixture, "submitter1");
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;
        var activityId = SeedActivity(fixture, submitterId);
        var (storage, rootPath) = MakeStorage();
        try
        {
            var service = MakeService(db, storage);
            var created = await service.CreateAsync(submitterId, MakeInput(themeId, activityId));

            var result = await service.AddAttachmentAsync(created.Idea!.Id, submitterId, "a.exe", "application/x-msdownload", new byte[] { 1 });

            Assert.Equal(IdeaCommandStatus.InvalidAttachment, result.Status);
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task AddAttachmentAsync_OversizedFile_ReturnsInvalidAttachment()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedSubmitter(fixture, "submitter1");
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;
        var activityId = SeedActivity(fixture, submitterId);
        var (storage, rootPath) = MakeStorage();
        try
        {
            var service = MakeService(db, storage);
            var created = await service.CreateAsync(submitterId, MakeInput(themeId, activityId));
            var oversized = new byte[IdeaAttachmentRules.MaxSizeBytes + 1];

            var result = await service.AddAttachmentAsync(created.Idea!.Id, submitterId, "big.pdf", "application/pdf", oversized);

            Assert.Equal(IdeaCommandStatus.InvalidAttachment, result.Status);
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task AddAttachmentAsync_ValidFile_PersistsAttachmentAndFile()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedSubmitter(fixture, "submitter1");
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;
        var activityId = SeedActivity(fixture, submitterId);
        var (storage, rootPath) = MakeStorage();
        try
        {
            var service = MakeService(db, storage);
            var created = await service.CreateAsync(submitterId, MakeInput(themeId, activityId));

            var result = await service.AddAttachmentAsync(created.Idea!.Id, submitterId, "a.pdf", "application/pdf", new byte[] { 1, 2, 3 });

            Assert.Equal(IdeaCommandStatus.Success, result.Status);
            Assert.True(File.Exists(result.Attachment!.BlobPath));

            var attachments = await service.GetAttachmentsAsync(created.Idea.Id, submitterId);
            Assert.Equal(IdeaCommandStatus.Success, attachments.Status);
            Assert.Single(attachments.Attachments);
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task GetAttachmentsAsync_WrongOwner_ReturnsForbidden()
    {
        using var fixture = new SqliteContextFixture();
        var ownerId = SeedSubmitter(fixture, "submitter1");
        var otherId = SeedSubmitter(fixture, "submitter2");
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;
        var activityId = SeedActivity(fixture, ownerId);
        var (storage, rootPath) = MakeStorage();
        try
        {
            var service = MakeService(db, storage);
            var created = await service.CreateAsync(ownerId, MakeInput(themeId, activityId));

            var result = await service.GetAttachmentsAsync(created.Idea!.Id, otherId);

            Assert.Equal(IdeaCommandStatus.Forbidden, result.Status);
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task GetAttachmentsAsync_WrongOwnerButElevatedReviewer_ReturnsSuccess()
    {
        using var fixture = new SqliteContextFixture();
        var ownerId = SeedSubmitter(fixture, "submitter1");
        var reviewerId = SeedSubmitter(fixture, "evaluator1");
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;
        var activityId = SeedActivity(fixture, ownerId);
        var (storage, rootPath) = MakeStorage();
        try
        {
            var service = MakeService(db, storage);
            var created = await service.CreateAsync(ownerId, MakeInput(themeId, activityId));

            var result = await service.GetAttachmentsAsync(created.Idea!.Id, reviewerId, isElevatedReviewer: true);

            Assert.Equal(IdeaCommandStatus.Success, result.Status);
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task AddAttachmentAsync_WhenPassAwaitingAttachments_Succeeds()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedSubmitter(fixture, "submitter1");
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;
        var activityId = SeedActivity(fixture, submitterId);
        var (storage, rootPath) = MakeStorage();
        try
        {
            var service = MakeService(db, storage);
            var created = await service.CreateAsync(submitterId, MakeInput(themeId, activityId));

            var passStatus = db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.PassAwaitingAttachments);
            var idea = db.Ideas.Single(i => i.Id == created.Idea!.Id);
            idea.IdeaStatusId = passStatus.Id;
            db.SaveChanges();

            var result = await service.AddAttachmentAsync(created.Idea!.Id, submitterId, "a.pdf", "application/pdf", new byte[] { 1 });

            Assert.Equal(IdeaCommandStatus.Success, result.Status);
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task AddAttachmentAsync_WhenSubmitted_ReturnsInvalidState()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedSubmitter(fixture, "submitter1");
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;
        var activityId = SeedActivity(fixture, submitterId);
        var (storage, rootPath) = MakeStorage();
        try
        {
            var service = MakeService(db, storage);
            var created = await service.CreateAsync(submitterId, MakeInput(themeId, activityId));
            await service.AddAttachmentAsync(created.Idea!.Id, submitterId, "a.pdf", "application/pdf", new byte[] { 1 });
            await service.SubmitAsync(created.Idea.Id, submitterId);

            var result = await service.AddAttachmentAsync(created.Idea.Id, submitterId, "b.pdf", "application/pdf", new byte[] { 2 });

            Assert.Equal(IdeaCommandStatus.InvalidState, result.Status);
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }

    [Theory]
    [InlineData("application/vnd.openxmlformats-officedocument.presentationml.presentation")]
    [InlineData("application/vnd.ms-powerpoint")]
    [InlineData("video/mp4")]
    [InlineData("video/quicktime")]
    public async Task AddAttachmentAsync_NewlyAllowedMimeType_ReturnsSuccess(string contentType)
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedSubmitter(fixture, "submitter1");
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;
        var activityId = SeedActivity(fixture, submitterId);
        var (storage, rootPath) = MakeStorage();
        try
        {
            var service = MakeService(db, storage);
            var created = await service.CreateAsync(submitterId, MakeInput(themeId, activityId));

            var result = await service.AddAttachmentAsync(created.Idea!.Id, submitterId, "file.bin", contentType, new byte[] { 1, 2, 3 });

            Assert.Equal(IdeaCommandStatus.Success, result.Status);
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task SubmitToCommitteeAsync_WithAttachment_TransitionsToCommittee()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedSubmitter(fixture, "submitter1");
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;
        var activityId = SeedActivity(fixture, submitterId);
        var (storage, rootPath) = MakeStorage();
        try
        {
            var service = MakeService(db, storage);
            var created = await service.CreateAsync(submitterId, MakeInput(themeId, activityId));

            var passStatus = db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.PassAwaitingAttachments);
            var idea = db.Ideas.Single(i => i.Id == created.Idea!.Id);
            idea.IdeaStatusId = passStatus.Id;
            db.SaveChanges();

            await service.AddAttachmentAsync(created.Idea!.Id, submitterId, "a.pdf", "application/pdf", new byte[] { 1 });

            var result = await service.SubmitToCommitteeAsync(created.Idea.Id, submitterId);

            Assert.Equal(IdeaCommandStatus.Success, result.Status);
            Assert.Equal(IdeaStatusCodes.Committee, result.Idea!.IdeaStatus.Code);
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task SubmitToCommitteeAsync_WithoutAttachment_ReturnsInvalidState()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedSubmitter(fixture, "submitter1");
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;
        var activityId = SeedActivity(fixture, submitterId);
        var (storage, rootPath) = MakeStorage();
        try
        {
            var service = MakeService(db, storage);
            var created = await service.CreateAsync(submitterId, MakeInput(themeId, activityId));

            var passStatus = db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.PassAwaitingAttachments);
            var idea = db.Ideas.Single(i => i.Id == created.Idea!.Id);
            idea.IdeaStatusId = passStatus.Id;
            db.SaveChanges();

            var result = await service.SubmitToCommitteeAsync(created.Idea!.Id, submitterId);

            Assert.Equal(IdeaCommandStatus.InvalidState, result.Status);
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task SubmitToCommitteeAsync_WrongOwner_ReturnsForbidden()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedSubmitter(fixture, "submitter1");
        var otherId = SeedSubmitter(fixture, "submitter2");
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;
        var activityId = SeedActivity(fixture, submitterId);
        var (storage, rootPath) = MakeStorage();
        try
        {
            var service = MakeService(db, storage);
            var created = await service.CreateAsync(submitterId, MakeInput(themeId, activityId));

            var passStatus = db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.PassAwaitingAttachments);
            var idea = db.Ideas.Single(i => i.Id == created.Idea!.Id);
            idea.IdeaStatusId = passStatus.Id;
            db.SaveChanges();

            var result = await service.SubmitToCommitteeAsync(created.Idea!.Id, otherId);

            Assert.Equal(IdeaCommandStatus.Forbidden, result.Status);
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task CreateAsync_ActivityDoesNotExist_ReturnsInvalidActivity()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedSubmitter(fixture, "submitter1");
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;
        var (storage, rootPath) = MakeStorage();
        try
        {
            var service = MakeService(db, storage);
            var result = await service.CreateAsync(submitterId, MakeInput(themeId, Guid.NewGuid()));

            Assert.Equal(IdeaCommandStatus.InvalidActivity, result.Status);
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task CreateAsync_ThemeHasActiveChallengeButNoneSelected_ReturnsInvalidChallenge()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedSubmitter(fixture, "submitter1");
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;
        var activityId = SeedActivity(fixture, submitterId);
        db.Challenges.Add(new Challenge { Id = Guid.NewGuid(), StrategicThemeId = themeId, TextAr = "ت", TextEn = "Challenge", SortOrder = 0, IsActive = true });
        db.SaveChanges();
        var (storage, rootPath) = MakeStorage();
        try
        {
            var service = MakeService(db, storage);
            var result = await service.CreateAsync(submitterId, MakeInput(themeId, activityId));

            Assert.Equal(IdeaCommandStatus.InvalidChallenge, result.Status);
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task CreateAsync_ChallengeBelongsToDifferentTheme_ReturnsInvalidChallenge()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedSubmitter(fixture, "submitter1");
        using var db = fixture.CreateContext();
        var themes = db.StrategicThemes.ToList();
        var themeId = themes[0].Id;
        var otherThemeId = themes[1].Id;
        var activityId = SeedActivity(fixture, submitterId);
        var challenge = new Challenge { Id = Guid.NewGuid(), StrategicThemeId = otherThemeId, TextAr = "ت", TextEn = "Challenge", SortOrder = 0, IsActive = true };
        db.Challenges.Add(challenge);
        db.SaveChanges();
        var (storage, rootPath) = MakeStorage();
        try
        {
            var service = MakeService(db, storage);
            var result = await service.CreateAsync(submitterId, MakeInput(themeId, activityId, challengeId: challenge.Id));

            Assert.Equal(IdeaCommandStatus.InvalidChallenge, result.Status);
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task CreateAsync_ChallengeIsInactive_ReturnsInvalidChallenge()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedSubmitter(fixture, "submitter1");
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;
        var activityId = SeedActivity(fixture, submitterId);
        var challenge = new Challenge { Id = Guid.NewGuid(), StrategicThemeId = themeId, TextAr = "ت", TextEn = "Challenge", SortOrder = 0, IsActive = false };
        db.Challenges.Add(challenge);
        db.SaveChanges();
        var (storage, rootPath) = MakeStorage();
        try
        {
            var service = MakeService(db, storage);
            var result = await service.CreateAsync(submitterId, MakeInput(themeId, activityId, challengeId: challenge.Id));

            Assert.Equal(IdeaCommandStatus.InvalidChallenge, result.Status);
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }

    [Theory]
    [InlineData("bogus")]
    [InlineData("")]
    public async Task CreateAsync_InvalidParticipationTypeValue_ReturnsInvalidParticipation(string participationType)
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedSubmitter(fixture, "submitter1");
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;
        var activityId = SeedActivity(fixture, submitterId);
        var (storage, rootPath) = MakeStorage();
        try
        {
            var service = MakeService(db, storage);
            var result = await service.CreateAsync(submitterId, MakeInput(themeId, activityId, participationType: participationType));

            Assert.Equal(IdeaCommandStatus.InvalidParticipation, result.Status);
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task CreateAsync_TeamWithOnlyOneAdditionalMember_ReturnsInvalidParticipation()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedSubmitter(fixture, "submitter1");
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;
        var activityId = SeedActivity(fixture, submitterId);
        var (storage, rootPath) = MakeStorage();
        try
        {
            var service = MakeService(db, storage);
            var input = MakeInput(themeId, activityId, participationType: "team", teamName: "Team A", teamMembers: new[] { new TeamMemberInput("Member One", "m1@example.com") });
            var result = await service.CreateAsync(submitterId, input);

            Assert.Equal(IdeaCommandStatus.InvalidParticipation, result.Status);
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task CreateAsync_TeamMemberInvalidEmail_ReturnsInvalidParticipation()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedSubmitter(fixture, "submitter1");
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;
        var activityId = SeedActivity(fixture, submitterId);
        var (storage, rootPath) = MakeStorage();
        try
        {
            var service = MakeService(db, storage);
            var members = new[] { new TeamMemberInput("Member One", "not-an-email"), new TeamMemberInput("Member Two", "m2@example.com") };
            var input = MakeInput(themeId, activityId, participationType: "team", teamName: "Team A", teamMembers: members);
            var result = await service.CreateAsync(submitterId, input);

            Assert.Equal(IdeaCommandStatus.InvalidParticipation, result.Status);
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task CreateAsync_ValidTeam_PersistsTeamMembersAndTeamName()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedSubmitter(fixture, "submitter1");
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;
        var activityId = SeedActivity(fixture, submitterId);
        var (storage, rootPath) = MakeStorage();
        try
        {
            var service = MakeService(db, storage);
            var members = new[] { new TeamMemberInput("Member One", "m1@example.com"), new TeamMemberInput("Member Two", "m2@example.com") };
            var input = MakeInput(themeId, activityId, participationType: "team", teamName: "Team A", teamMembers: members);
            var result = await service.CreateAsync(submitterId, input);

            Assert.Equal(IdeaCommandStatus.Success, result.Status);
            Assert.Equal("Team A", result.Idea!.TeamName);

            using var readDb = fixture.CreateContext();
            var savedMembers = readDb.IdeaTeamMembers.Where(m => m.IdeaId == result.Idea.Id).ToList();
            Assert.Equal(2, savedMembers.Count);
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public async Task CreateAsync_ConsentNotGiven_ReturnsConsentRequired()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedSubmitter(fixture, "submitter1");
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;
        var activityId = SeedActivity(fixture, submitterId);
        var (storage, rootPath) = MakeStorage();
        try
        {
            var service = MakeService(db, storage);
            var result = await service.CreateAsync(submitterId, MakeInput(themeId, activityId, ipAcknowledged: false));

            Assert.Equal(IdeaCommandStatus.ConsentRequired, result.Status);
        }
        finally
        {
            if (Directory.Exists(rootPath)) Directory.Delete(rootPath, recursive: true);
        }
    }
}
