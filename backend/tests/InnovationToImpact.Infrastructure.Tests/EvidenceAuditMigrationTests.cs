using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

[Trait("Category", "Integration")]
public class EvidenceAuditMigrationTests
{
    private const string ConnectionString =
        "Server=(localdb)\\mssqllocaldb;Database=InnovationToImpact_Test;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

    [Fact]
    public void MigratedDatabaseAcceptsAnAuditLogRow()
    {
        var options = new DbContextOptionsBuilder<InnovationDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        using var context = new InnovationDbContext(options);
        context.Database.Migrate();

        var chainSeq = DateTime.UtcNow.Ticks;
        context.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            ChainSeq = chainSeq,
            RowHash = new string('e', 64),
            EntityType = "idea",
            EntityId = Guid.NewGuid(),
            Action = "create",
        });
        context.SaveChanges();

        Assert.True(context.AuditLogs.Any(a => a.ChainSeq == chainSeq));
    }
}
