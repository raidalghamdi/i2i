using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

[Trait("Category", "Integration")]
public class StrategicThemeSeedMigrationTests
{
    private const string ConnectionString =
        "Server=(localdb)\\mssqllocaldb;Database=InnovationToImpact_Test;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

    [Fact]
    public void MigratedDatabaseContainsSeededThemesAndSystemUser()
    {
        var options = new DbContextOptionsBuilder<InnovationDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        using var context = new InnovationDbContext(options);
        context.Database.Migrate();

        var systemUser = context.Users.SingleOrDefault(u => u.SamAccountName == "system");
        Assert.NotNull(systemUser);

        var themeCount = context.StrategicThemes.Count(t => t.OwnerId == systemUser!.Id);
        Assert.True(themeCount >= 3);
    }
}
