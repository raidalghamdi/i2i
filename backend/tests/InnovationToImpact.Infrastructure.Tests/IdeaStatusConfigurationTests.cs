using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class IdeaStatusConfigurationTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public IdeaStatusConfigurationTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void SeedsAllTwentyTwoStatusCodes()
    {
        using var context = _fixture.CreateContext();
        var codes = context.IdeaStatuses.Select(s => s.Code).ToList();

        Assert.Equal(22, codes.Count);
        Assert.Contains("draft", codes);
        Assert.Contains("submitted", codes);
        Assert.Contains("pass_awaiting_attachments", codes);
        Assert.Contains("in_scaling", codes);
    }

    [Fact]
    public void RejectsDuplicateStatusCode()
    {
        using var context = _fixture.CreateContext();
        context.IdeaStatuses.Add(new IdeaStatus { Id = Guid.NewGuid(), Code = "draft", NameAr = "مسودة", NameEn = "Draft", SortOrder = 99 });

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }
}
