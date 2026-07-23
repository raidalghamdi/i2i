using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class ApprovalLookupTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public ApprovalLookupTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void SeedsAllFourApprovalInstanceStatusCodes()
    {
        using var context = _fixture.CreateContext();
        var codes = context.ApprovalInstanceStatuses.Select(s => s.Code).ToList();

        Assert.Equal(4, codes.Count);
        Assert.Contains("pending", codes);
        Assert.Contains("approved", codes);
        Assert.Contains("rejected", codes);
        Assert.Contains("cancelled", codes);
    }

    [Fact]
    public void SeedsAllThreeApprovalDecisionTypeCodes()
    {
        using var context = _fixture.CreateContext();
        var codes = context.ApprovalDecisionTypes.Select(d => d.Code).ToList();

        Assert.Equal(3, codes.Count);
        Assert.Contains("approve", codes);
        Assert.Contains("reject", codes);
        Assert.Contains("request_changes", codes);
    }

    [Fact]
    public void RejectsDuplicateApprovalDecisionTypeCode()
    {
        using var context = _fixture.CreateContext();
        context.ApprovalDecisionTypes.Add(new ApprovalDecisionType { Id = Guid.NewGuid(), Code = "approve", NameAr = "مكرر", NameEn = "Dup", SortOrder = 99 });

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }
}
