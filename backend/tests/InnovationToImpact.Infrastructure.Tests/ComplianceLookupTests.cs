using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class ComplianceLookupTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public ComplianceLookupTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void SeedsAllFiveStandardBodyCodes()
    {
        using var context = _fixture.CreateContext();
        var codes = context.StandardBodies.Select(b => b.Code).ToList();

        Assert.Equal(5, codes.Count);
        Assert.Contains("sdaia_ndmo", codes);
        Assert.Contains("nca", codes);
        Assert.Contains("dga", codes);
        Assert.Contains("cst", codes);
        Assert.Contains("rdia", codes);
    }

    [Fact]
    public void SeedsAllFourComplianceControlStatusCodes()
    {
        using var context = _fixture.CreateContext();
        var codes = context.ComplianceControlStatuses.Select(s => s.Code).ToList();

        Assert.Equal(4, codes.Count);
        Assert.Contains("not_started", codes);
        Assert.Contains("in_progress", codes);
        Assert.Contains("met", codes);
        Assert.Contains("not_applicable", codes);
    }

    [Fact]
    public void RejectsDuplicateStandardBodyCode()
    {
        using var context = _fixture.CreateContext();
        context.StandardBodies.Add(new StandardBody { Id = Guid.NewGuid(), Code = "nca", NameAr = "مكرر", NameEn = "Dup", SortOrder = 99 });

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }
}
