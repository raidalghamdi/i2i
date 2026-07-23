using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class AssignmentStatusAndCommitteeDecisionTypeTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public AssignmentStatusAndCommitteeDecisionTypeTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void SeedsAllThreeAssignmentStatusCodes()
    {
        using var context = _fixture.CreateContext();
        var codes = context.AssignmentStatuses.Select(s => s.Code).ToList();

        Assert.Equal(3, codes.Count);
        Assert.Contains("pending", codes);
        Assert.Contains("completed", codes);
        Assert.Contains("declined", codes);
    }

    [Fact]
    public void SeedsAllThreeCommitteeDecisionTypeCodes()
    {
        using var context = _fixture.CreateContext();
        var codes = context.CommitteeDecisionTypes.Select(t => t.Code).ToList();

        Assert.Equal(3, codes.Count);
        Assert.Contains("approved", codes);
        Assert.Contains("rejected", codes);
        Assert.Contains("deferred", codes);
    }

    [Fact]
    public void RejectsDuplicateAssignmentStatusCode()
    {
        using var context = _fixture.CreateContext();
        context.AssignmentStatuses.Add(new AssignmentStatus { Id = Guid.NewGuid(), Code = "pending", NameAr = "معلق", NameEn = "Pending", SortOrder = 99 });

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }
}
