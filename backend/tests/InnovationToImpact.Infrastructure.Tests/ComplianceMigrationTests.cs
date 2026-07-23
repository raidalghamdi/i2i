using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

[Trait("Category", "Integration")]
public class ComplianceMigrationTests
{
    private const string ConnectionString =
        "Server=(localdb)\\mssqllocaldb;Database=InnovationToImpact_Test;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

    [Fact]
    public void MigratedDatabaseContainsComplianceSeedData()
    {
        var options = new DbContextOptionsBuilder<InnovationDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        using var context = new InnovationDbContext(options);
        context.Database.Migrate();

        Assert.Equal(5, context.StandardBodies.Count());
        Assert.Equal(4, context.ComplianceControlStatuses.Count());
    }
}
