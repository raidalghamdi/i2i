using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class CommitteeCriterionConfigurationTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public CommitteeCriterionConfigurationTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void SeedsFourWeightedCriteriaSummingToOne()
    {
        using var context = _fixture.CreateContext();
        var criteria = context.CommitteeCriteria.ToList();

        Assert.Equal(4, criteria.Count);
        Assert.Equal(1.00m, criteria.Sum(c => c.Weight));

        var originality = criteria.Single(c => c.Code == "originality");
        Assert.Equal(0.30m, originality.Weight);
        Assert.True(originality.Active);
    }

    [Fact]
    public void RejectsDuplicateCriterionCode()
    {
        using var context = _fixture.CreateContext();
        context.CommitteeCriteria.Add(new CommitteeCriterion { Id = Guid.NewGuid(), Code = "originality", NameAr = "أ", NameEn = "Dup", Weight = 0.10m });

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }
}
