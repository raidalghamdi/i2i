using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class AuditLogConfigurationTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public AuditLogConfigurationTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void SavesChainedEntryAndAllowsNullActorForSystemEvents()
    {
        Guid firstId;
        Guid secondId;

        using (var context = _fixture.CreateContext())
        {
            var first = new AuditLog
            {
                Id = Guid.NewGuid(),
                ChainSeq = 1,
                RowHash = new string('a', 64),
                PrevHash = null,
                EntityType = "idea",
                EntityId = Guid.NewGuid(),
                Action = "create",
                ActorId = null,
            };
            firstId = first.Id;

            context.AuditLogs.Add(first);
            context.SaveChanges();

            var second = new AuditLog
            {
                Id = Guid.NewGuid(),
                ChainSeq = 2,
                RowHash = new string('b', 64),
                PrevHash = first.RowHash,
                EntityType = "idea",
                EntityId = first.EntityId,
                Action = "update",
                ActorId = null,
            };
            secondId = second.Id;

            context.AuditLogs.Add(second);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var first = context.AuditLogs.Single(a => a.Id == firstId);
            var second = context.AuditLogs.Single(a => a.Id == secondId);

            Assert.Null(first.PrevHash);
            Assert.Equal(first.RowHash, second.PrevHash);
            Assert.Null(second.ActorId);
        }
    }

    [Fact]
    public void RejectsDuplicateChainSeq()
    {
        using var context = _fixture.CreateContext();

        context.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), ChainSeq = 100, RowHash = new string('c', 64), EntityType = "idea", EntityId = Guid.NewGuid(), Action = "create" });
        context.SaveChanges();

        context.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), ChainSeq = 100, RowHash = new string('d', 64), EntityType = "idea", EntityId = Guid.NewGuid(), Action = "create" });

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }
}
