using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

[Trait("Category", "Integration")]
public class CmsContentReportingMigrationTests
{
    private const string ConnectionString =
        "Server=(localdb)\\mssqllocaldb;Database=InnovationToImpact_Test;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

    [Fact]
    public void MigratedDatabaseAcceptsAContentStringRow()
    {
        var options = new DbContextOptionsBuilder<InnovationDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        using var context = new InnovationDbContext(options);
        context.Database.Migrate();

        var key = $"integration-check-{Guid.NewGuid()}";
        context.ContentStrings.Add(new ContentString { Id = Guid.NewGuid(), Key = key, ValueAr = "أ", ValueEn = "A" });
        context.SaveChanges();

        Assert.True(context.ContentStrings.Any(s => s.Key == key));
    }
}
