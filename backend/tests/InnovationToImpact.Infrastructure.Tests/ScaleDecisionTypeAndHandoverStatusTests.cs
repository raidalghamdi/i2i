using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class ScaleDecisionTypeAndHandoverStatusTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public ScaleDecisionTypeAndHandoverStatusTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void SeedsAllThreeScaleDecisionTypeCodes()
    {
        using var context = _fixture.CreateContext();
        var codes = context.ScaleDecisionTypes.Select(t => t.Code).ToList();

        Assert.Equal(3, codes.Count);
        Assert.Contains("scale", codes);
        Assert.Contains("hold", codes);
        Assert.Contains("reject", codes);
    }

    [Fact]
    public void SeedsAllThreeHandoverStatusCodes()
    {
        using var context = _fixture.CreateContext();
        var codes = context.HandoverStatuses.Select(s => s.Code).ToList();

        Assert.Equal(3, codes.Count);
        Assert.Contains("pending", codes);
        Assert.Contains("in_progress", codes);
        Assert.Contains("completed", codes);
    }

    [Fact]
    public void RejectsDuplicateScaleDecisionTypeCode()
    {
        using var context = _fixture.CreateContext();
        context.ScaleDecisionTypes.Add(new ScaleDecisionType { Id = Guid.NewGuid(), Code = "scale", NameAr = "مكرر", NameEn = "Dup", SortOrder = 99 });

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }
}
