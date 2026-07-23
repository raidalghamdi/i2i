using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

[Trait("Category", "Integration")]
public class GamificationCmsMigrationTests
{
    private const string ConnectionString =
        "Server=(localdb)\\mssqllocaldb;Database=InnovationToImpact_Test;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

    [Fact]
    public void MigratedDatabaseAcceptsABadgeRow()
    {
        var options = new DbContextOptionsBuilder<InnovationDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        using var context = new InnovationDbContext(options);
        context.Database.Migrate();

        var code = $"integration-check-{Guid.NewGuid()}";
        context.Badges.Add(new Badge { Id = Guid.NewGuid(), Code = code, NameAr = "أ", NameEn = "A" });
        context.SaveChanges();

        Assert.True(context.Badges.Any(b => b.Code == code));
    }
}
