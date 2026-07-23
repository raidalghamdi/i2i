using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

[Trait("Category", "Integration")]
public class SlaEscalationsMigrationTests
{
    private const string ConnectionString =
        "Server=(localdb)\\mssqllocaldb;Database=InnovationToImpact_Test;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

    [Fact]
    public void MigratedDatabaseContainsSlaEscalationsSeedData()
    {
        var options = new DbContextOptionsBuilder<InnovationDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        using var context = new InnovationDbContext(options);
        context.Database.Migrate();

        Assert.Equal(3, context.EscalationTiers.Count());
        Assert.Equal(4, context.EscalationStatuses.Count());
    }
}
