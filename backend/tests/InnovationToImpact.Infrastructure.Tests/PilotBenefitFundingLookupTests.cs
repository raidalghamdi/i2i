using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class PilotBenefitFundingLookupTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public PilotBenefitFundingLookupTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void SeedsAllFourPilotStatusCodes()
    {
        using var context = _fixture.CreateContext();
        var codes = context.PilotStatuses.Select(s => s.Code).ToList();

        Assert.Equal(4, codes.Count);
        Assert.Contains("planned", codes);
        Assert.Contains("in_progress", codes);
        Assert.Contains("completed", codes);
        Assert.Contains("cancelled", codes);
    }

    [Fact]
    public void SeedsBothBenefitTypeCodes()
    {
        using var context = _fixture.CreateContext();
        var codes = context.BenefitTypes.Select(t => t.Code).ToList();

        Assert.Equal(2, codes.Count);
        Assert.Contains("quantitative", codes);
        Assert.Contains("qualitative", codes);
    }

    [Fact]
    public void SeedsAllFourBenefitCategoryCodes()
    {
        using var context = _fixture.CreateContext();
        var codes = context.BenefitCategories.Select(c => c.Code).ToList();

        Assert.Equal(4, codes.Count);
        Assert.Contains("financial", codes);
        Assert.Contains("operational", codes);
        Assert.Contains("social", codes);
        Assert.Contains("strategic", codes);
    }

    [Fact]
    public void SeedsAllFourFundingStatusCodes()
    {
        using var context = _fixture.CreateContext();
        var codes = context.FundingStatuses.Select(s => s.Code).ToList();

        Assert.Equal(4, codes.Count);
        Assert.Contains("pending", codes);
        Assert.Contains("approved", codes);
        Assert.Contains("rejected", codes);
        Assert.Contains("partially_approved", codes);
    }

    [Fact]
    public void RejectsDuplicatePilotStatusCode()
    {
        using var context = _fixture.CreateContext();
        context.PilotStatuses.Add(new PilotStatus { Id = Guid.NewGuid(), Code = "planned", NameAr = "مكرر", NameEn = "Dup", SortOrder = 99 });

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }
}
