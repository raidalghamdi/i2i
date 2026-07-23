using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class EscalationLookupTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public EscalationLookupTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void SeedsAllThreeEscalationTierCodes()
    {
        using var context = _fixture.CreateContext();
        var codes = context.EscalationTiers.Select(t => t.Code).ToList();

        Assert.Equal(3, codes.Count);
        Assert.Contains("manager", codes);
        Assert.Contains("director", codes);
        Assert.Contains("exec", codes);
    }

    [Fact]
    public void SeedsAllFourEscalationStatusCodes()
    {
        using var context = _fixture.CreateContext();
        var codes = context.EscalationStatuses.Select(s => s.Code).ToList();

        Assert.Equal(4, codes.Count);
        Assert.Contains("open", codes);
        Assert.Contains("acknowledged", codes);
        Assert.Contains("resolved", codes);
        Assert.Contains("cancelled", codes);
    }

    [Fact]
    public void RejectsDuplicateEscalationTierCode()
    {
        using var context = _fixture.CreateContext();
        context.EscalationTiers.Add(new EscalationTier { Id = Guid.NewGuid(), Code = "manager", NameAr = "مكرر", NameEn = "Dup", SortOrder = 99 });

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }
}
