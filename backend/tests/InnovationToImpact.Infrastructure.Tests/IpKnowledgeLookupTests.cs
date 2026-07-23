using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class IpKnowledgeLookupTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public IpKnowledgeLookupTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void SeedsAllFourIpTypeCodes()
    {
        using var context = _fixture.CreateContext();
        var codes = context.IpTypes.Select(t => t.Code).ToList();

        Assert.Equal(4, codes.Count);
        Assert.Contains("patent", codes);
        Assert.Contains("trademark", codes);
        Assert.Contains("copyright", codes);
        Assert.Contains("trade_secret", codes);
    }

    [Fact]
    public void SeedsAllFourKnowledgeTypeCodes()
    {
        using var context = _fixture.CreateContext();
        var codes = context.KnowledgeTypes.Select(t => t.Code).ToList();

        Assert.Equal(4, codes.Count);
        Assert.Contains("article", codes);
        Assert.Contains("case_study", codes);
        Assert.Contains("template", codes);
        Assert.Contains("official_guide", codes);
    }

    [Fact]
    public void SeedsBothKnowledgeVisibilityCodes()
    {
        using var context = _fixture.CreateContext();
        var codes = context.KnowledgeVisibilities.Select(v => v.Code).ToList();

        Assert.Equal(2, codes.Count);
        Assert.Contains("public", codes);
        Assert.Contains("internal", codes);
    }

    [Fact]
    public void RejectsDuplicateIpTypeCode()
    {
        using var context = _fixture.CreateContext();
        context.IpTypes.Add(new IpType { Id = Guid.NewGuid(), Code = "patent", NameAr = "مكرر", NameEn = "Dup", SortOrder = 99 });

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }
}
