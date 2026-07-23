using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

[Trait("Category", "Integration")]
public class EvaluationCommitteeMigrationTests
{
    private const string ConnectionString =
        "Server=(localdb)\\mssqllocaldb;Database=InnovationToImpact_Test;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

    [Fact]
    public void MigratedDatabaseContainsEvaluationCommitteeSeedData()
    {
        var options = new DbContextOptionsBuilder<InnovationDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        using var context = new InnovationDbContext(options);
        context.Database.Migrate();

        Assert.Equal(3, context.AssignmentStatuses.Count());
        Assert.Equal(3, context.CommitteeDecisionTypes.Count());
        Assert.Equal(4, context.CommitteeCriteria.Count());
        Assert.Equal(1.00m, context.CommitteeCriteria.Sum(c => c.Weight));
        Assert.Equal(2, context.AdminSettings.Count());
    }
}
