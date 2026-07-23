using InnovationToImpact.Domain.Auth;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

[Trait("Category", "Integration")]
public class RoleMigrationTests
{
    private const string ConnectionString =
        "Server=(localdb)\\mssqllocaldb;Database=InnovationToImpact_Test;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

    [Fact]
    public void MigratedDatabaseContainsCanonicalRoleSeed()
    {
        var options = new DbContextOptionsBuilder<InnovationDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        using var context = new InnovationDbContext(options);
        context.Database.Migrate();

        Assert.Equal(8, context.Roles.Count());
        Assert.True(context.Roles.Any(r => r.Code == RoleCodes.Admin));
        Assert.True(context.Roles.Any(r => r.Code == RoleCodes.Supervisor));
        Assert.True(context.Roles.Any(r => r.Code == RoleCodes.Judge));
        Assert.True(context.Roles.Any(r => r.Code == RoleCodes.Evaluator));
        Assert.True(context.Roles.Any(r => r.Code == RoleCodes.Submitter));
        Assert.True(context.Roles.Any(r => r.Code == RoleCodes.Expert));
        Assert.True(context.Roles.Any(r => r.Code == RoleCodes.Mentor));
        Assert.True(context.Roles.Any(r => r.Code == RoleCodes.Facilitator));
        Assert.All(context.Roles, r => Assert.True(r.IsSystem && r.IsActive));
    }
}
