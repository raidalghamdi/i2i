using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class EmailLogConfigurationTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public EmailLogConfigurationTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    private static Guid SeedSentStatus(InnovationDbContext context)
    {
        return context.EmailLogStatuses.Single(s => s.Code == "sent").Id;
    }

    [Fact]
    public void SavesEmailLogWithPolymorphicRelatedEntity()
    {
        Guid logId;
        Guid relatedIdeaLikeId;

        using (var context = _fixture.CreateContext())
        {
            var sentStatusId = SeedSentStatus(context);
            relatedIdeaLikeId = Guid.NewGuid();

            var log = new EmailLog
            {
                Id = Guid.NewGuid(),
                Provider = "resend",
                EmailLogStatusId = sentStatusId,
                ProviderMessageId = "provider-msg-1",
                RedirectApplied = false,
                RelatedEntityType = "invitation",
                RelatedEntityId = relatedIdeaLikeId,
                ToEmail = "recipient-t4a@example.com",
            };
            logId = log.Id;

            context.EmailLogs.Add(log);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var log = context.EmailLogs
                .Include(l => l.EmailLogStatus)
                .Single(l => l.Id == logId);
            Assert.Equal("sent", log.EmailLogStatus.Code);
            Assert.Equal("invitation", log.RelatedEntityType);
            Assert.Equal(relatedIdeaLikeId, log.RelatedEntityId);
        }
    }

    [Fact]
    public void AllowsNullToUserIdAndNullRelatedEntityForAGenericLogEntry()
    {
        using var context = _fixture.CreateContext();
        var sentStatusId = SeedSentStatus(context);

        context.EmailLogs.Add(new EmailLog
        {
            Id = Guid.NewGuid(),
            Provider = "smtp",
            EmailLogStatusId = sentStatusId,
            RedirectApplied = false,
            ToEmail = "recipient-t4b@example.com",
            ToUserId = null,
            RelatedEntityType = null,
            RelatedEntityId = null,
        });

        var exception = Record.Exception(() => context.SaveChanges());
        Assert.Null(exception);
    }
}
