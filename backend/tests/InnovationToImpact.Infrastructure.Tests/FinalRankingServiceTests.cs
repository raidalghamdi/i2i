using InnovationToImpact.Domain.Audit;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Ideas;
using InnovationToImpact.Domain.Notifications;
using InnovationToImpact.Infrastructure.Approvals;
using InnovationToImpact.Infrastructure.Data;
using InnovationToImpact.Infrastructure.FinalRanking;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class FinalRankingServiceTests
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

    // Real ApprovalService (backed by the same fixture db) rather than a no-op fake, so the new
    // gate-wiring test below can assert an idea-approve ApprovalInstance was actually created.
    private static FinalRankingService MakeFinalRankingService(InnovationDbContext db) =>
        new(db, new ApprovalService(db, new FakeAuditLogWriter(), new FakeNotificationService()));

    private static Guid SeedSubmitter(SqliteContextFixture fixture)
    {
        using var db = fixture.CreateContext();
        var id = Guid.NewGuid();
        db.Users.Add(new User { Id = id, SamAccountName = "submitter1", Email = "submitter1@gac-demo.sa", FullNameAr = "submitter1", FullNameEn = "submitter1" });
        db.SaveChanges();
        return id;
    }

    private static Idea SeedIdeaPendingFinalRanking(SqliteContextFixture fixture, Guid submitterId, Guid themeId, string code, decimal? score, DateTime createdAt)
    {
        using var db = fixture.CreateContext();
        var status = db.IdeaStatuses.Single(s => s.Code == IdeaStatusCodes.PendingFinalRanking);
        var idea = new Idea
        {
            Id = Guid.NewGuid(),
            Code = code,
            TitleAr = "ا",
            TitleEn = code,
            ProblemStatementAr = "م",
            ProblemStatementEn = "P",
            ProposedSolutionAr = "ح",
            ProposedSolutionEn = "S",
            ExpectedBenefitsAr = "ف",
            ExpectedBenefitsEn = "B",
            StrategicThemeId = themeId,
            IdeaStatusId = status.Id,
            CurrentStage = 5,
            SubmitterId = submitterId,
            CommitteeFinalScore = score,
            CreatedAt = createdAt,
        };
        db.Ideas.Add(idea);
        db.SaveChanges();
        return idea;
    }

    [Fact]
    public async Task PreviewAsync_DoesNotPersistAnyChanges()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedSubmitter(fixture);
        using var themeDb = fixture.CreateContext();
        var themeId = themeDb.StrategicThemes.First().Id;
        SeedIdeaPendingFinalRanking(fixture, submitterId, themeId, "IDEA-0001", 9m, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        using var db = fixture.CreateContext();
        var service = MakeFinalRankingService(db);

        var result = await service.PreviewAsync();

        Assert.Equal(1, result.ApprovedCount);
        Assert.Equal(5, result.TopN);

        using var verifyDb = fixture.CreateContext();
        var reloaded = verifyDb.Ideas.Include(i => i.IdeaStatus).Single();
        Assert.Equal(IdeaStatusCodes.PendingFinalRanking, reloaded.IdeaStatus.Code);
        Assert.Null(reloaded.FinalRank);
        Assert.Null(reloaded.ApprovedAt);
    }

    [Fact]
    public async Task RunAsync_SingleTrack_TopNApprovedRestNotSelected()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedSubmitter(fixture);
        using var themeDb = fixture.CreateContext();
        var themeId = themeDb.StrategicThemes.First().Id;

        using (var adminDb = fixture.CreateContext())
        {
            var existing = adminDb.AdminSettings.Single(s => s.Key == "top_n");
            existing.ValueJson = "2";
            adminDb.SaveChanges();
        }

        var idea1 = SeedIdeaPendingFinalRanking(fixture, submitterId, themeId, "IDEA-0001", 9m, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var idea2 = SeedIdeaPendingFinalRanking(fixture, submitterId, themeId, "IDEA-0002", 7m, new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc));
        var idea3 = SeedIdeaPendingFinalRanking(fixture, submitterId, themeId, "IDEA-0003", 5m, new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc));

        using var db = fixture.CreateContext();
        var service = MakeFinalRankingService(db);
        var supervisorId = Guid.NewGuid();

        var result = await service.RunAsync(supervisorId);

        Assert.Equal(2, result.ApprovedCount);
        Assert.Equal(1, result.NotSelectedCount);
        Assert.Equal(2, result.TopN);

        using var verifyDb = fixture.CreateContext();
        var reloaded1 = verifyDb.Ideas.Include(i => i.IdeaStatus).Single(i => i.Id == idea1.Id);
        var reloaded2 = verifyDb.Ideas.Include(i => i.IdeaStatus).Single(i => i.Id == idea2.Id);
        var reloaded3 = verifyDb.Ideas.Include(i => i.IdeaStatus).Single(i => i.Id == idea3.Id);

        Assert.Equal(IdeaStatusCodes.Approved, reloaded1.IdeaStatus.Code);
        Assert.Equal(1, reloaded1.FinalRank);
        Assert.NotNull(reloaded1.ApprovedAt);

        Assert.Equal(IdeaStatusCodes.Approved, reloaded2.IdeaStatus.Code);
        Assert.Equal(2, reloaded2.FinalRank);

        Assert.Equal(IdeaStatusCodes.NotSelected, reloaded3.IdeaStatus.Code);
        Assert.Equal(3, reloaded3.FinalRank);
        Assert.Null(reloaded3.ApprovedAt);
    }

    [Fact]
    public async Task RunAsync_TiedScores_TiebreaksByCreatedAtAscending()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedSubmitter(fixture);
        using var themeDb = fixture.CreateContext();
        var themeId = themeDb.StrategicThemes.First().Id;

        using (var adminDb = fixture.CreateContext())
        {
            var existing = adminDb.AdminSettings.Single(s => s.Key == "top_n");
            existing.ValueJson = "1";
            adminDb.SaveChanges();
        }

        var earlier = SeedIdeaPendingFinalRanking(fixture, submitterId, themeId, "IDEA-0001", 8m, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var later = SeedIdeaPendingFinalRanking(fixture, submitterId, themeId, "IDEA-0002", 8m, new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc));

        using var db = fixture.CreateContext();
        var service = MakeFinalRankingService(db);

        await service.RunAsync(Guid.NewGuid());

        using var verifyDb = fixture.CreateContext();
        var reloadedEarlier = verifyDb.Ideas.Include(i => i.IdeaStatus).Single(i => i.Id == earlier.Id);
        var reloadedLater = verifyDb.Ideas.Include(i => i.IdeaStatus).Single(i => i.Id == later.Id);

        Assert.Equal(IdeaStatusCodes.Approved, reloadedEarlier.IdeaStatus.Code);
        Assert.Equal(IdeaStatusCodes.NotSelected, reloadedLater.IdeaStatus.Code);
    }

    [Fact]
    public async Task RunAsync_MultipleTracks_RankIndependently()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedSubmitter(fixture);
        using var themeDb = fixture.CreateContext();
        var themes = themeDb.StrategicThemes.Take(2).ToList();
        var trackA = themes[0].Id;
        var trackB = themes[1].Id;

        using (var adminDb = fixture.CreateContext())
        {
            var existing = adminDb.AdminSettings.Single(s => s.Key == "top_n");
            existing.ValueJson = "1";
            adminDb.SaveChanges();
        }

        var trackAIdea = SeedIdeaPendingFinalRanking(fixture, submitterId, trackA, "IDEA-0001", 3m, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var trackBIdea = SeedIdeaPendingFinalRanking(fixture, submitterId, trackB, "IDEA-0002", 3m, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        using var db = fixture.CreateContext();
        var service = MakeFinalRankingService(db);

        var result = await service.RunAsync(Guid.NewGuid());

        Assert.Equal(2, result.ApprovedCount);

        using var verifyDb = fixture.CreateContext();
        Assert.Equal(IdeaStatusCodes.Approved, verifyDb.Ideas.Include(i => i.IdeaStatus).Single(i => i.Id == trackAIdea.Id).IdeaStatus.Code);
        Assert.Equal(IdeaStatusCodes.Approved, verifyDb.Ideas.Include(i => i.IdeaStatus).Single(i => i.Id == trackBIdea.Id).IdeaStatus.Code);
    }

    [Fact]
    public async Task RunAsync_NoAdminSettingRow_DefaultsTopNToFive()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedSubmitter(fixture);
        using var themeDb = fixture.CreateContext();
        var themeId = themeDb.StrategicThemes.First().Id;
        SeedIdeaPendingFinalRanking(fixture, submitterId, themeId, "IDEA-0001", 9m, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        using (var adminDb = fixture.CreateContext())
        {
            var existing = adminDb.AdminSettings.Single(s => s.Key == "top_n");
            adminDb.AdminSettings.Remove(existing);
            adminDb.SaveChanges();
        }

        using var db = fixture.CreateContext();
        var service = MakeFinalRankingService(db);

        var result = await service.RunAsync(Guid.NewGuid());

        Assert.Equal(5, result.TopN);
    }

    [Fact]
    public async Task RunAsync_IdeaBecomesApproved_OpensIdeaApproveApprovalInstance()
    {
        using var fixture = new SqliteContextFixture();
        var submitterId = SeedSubmitter(fixture);
        using var themeDb = fixture.CreateContext();
        var themeId = themeDb.StrategicThemes.First().Id;

        using (var adminDb = fixture.CreateContext())
        {
            var existing = adminDb.AdminSettings.Single(s => s.Key == "top_n");
            existing.ValueJson = "1";
            adminDb.SaveChanges();
        }

        var approvedIdea = SeedIdeaPendingFinalRanking(fixture, submitterId, themeId, "IDEA-0001", 9m, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var notSelectedIdea = SeedIdeaPendingFinalRanking(fixture, submitterId, themeId, "IDEA-0002", 5m, new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc));

        using var db = fixture.CreateContext();
        var service = MakeFinalRankingService(db);

        var result = await service.RunAsync(Guid.NewGuid());

        Assert.Equal(1, result.ApprovedCount);

        using var verifyDb = fixture.CreateContext();
        var instance = await verifyDb.ApprovalInstances
            .Include(i => i.ApprovalChain)
            .Include(i => i.ApprovalInstanceStatus)
            .SingleAsync(i => i.EntityType == "idea" && i.EntityId == approvedIdea.Id);
        Assert.Equal("idea-approve", instance.ApprovalChain.Code);
        Assert.Equal("pending", instance.ApprovalInstanceStatus.Code);

        Assert.False(await verifyDb.ApprovalInstances.AnyAsync(i => i.EntityType == "idea" && i.EntityId == notSelectedIdea.Id));
    }
}
