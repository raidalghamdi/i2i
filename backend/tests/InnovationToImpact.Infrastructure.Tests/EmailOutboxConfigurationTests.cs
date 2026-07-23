using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class EmailOutboxConfigurationTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public EmailOutboxConfigurationTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    private static Guid SeedPendingStatus(InnovationDbContext context)
    {
        return context.EmailOutboxStatuses.Single(s => s.Code == "pending").Id;
    }

    [Fact]
    public void SavesOutboxEntryTargetingAPlatformUser()
    {
        Guid outboxId;
        Guid userId;

        using (var context = _fixture.CreateContext())
        {
            var pendingStatusId = SeedPendingStatus(context);

            userId = Guid.NewGuid();
            context.Users.Add(new User { Id = userId, SamAccountName = "outbox-t3a", Email = "outbox-t3a@gac-demo.sa", FullNameAr = "أ", FullNameEn = "User" });
            context.SaveChanges();

            var outbox = new EmailOutbox
            {
                Id = Guid.NewGuid(),
                ToEmail = "outbox-t3a@gac-demo.sa",
                ToUserId = userId,
                Subject = "Subject",
                BodyHtml = "<p>Body</p>",
                Category = "idea_lifecycle",
                EmailOutboxStatusId = pendingStatusId,
            };
            outboxId = outbox.Id;

            context.EmailOutboxes.Add(outbox);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var outbox = context.EmailOutboxes
                .Include(o => o.EmailOutboxStatus)
                .Single(o => o.Id == outboxId);
            Assert.Equal("pending", outbox.EmailOutboxStatus.Code);
            Assert.Equal(userId, outbox.ToUserId);
            Assert.Equal(0, outbox.Attempts);
        }
    }

    [Fact]
    public void AllowsNullToUserIdForAnExternalRecipient()
    {
        using var context = _fixture.CreateContext();
        var pendingStatusId = SeedPendingStatus(context);

        context.EmailOutboxes.Add(new EmailOutbox
        {
            Id = Guid.NewGuid(),
            ToEmail = "external@example.com",
            ToUserId = null,
            Subject = "Subject",
            BodyHtml = "<p>Body</p>",
            Category = "invitation",
            EmailOutboxStatusId = pendingStatusId,
        });

        var exception = Record.Exception(() => context.SaveChanges());
        Assert.Null(exception);
    }
}
