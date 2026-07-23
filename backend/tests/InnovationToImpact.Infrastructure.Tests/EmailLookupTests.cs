using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class EmailLookupTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public EmailLookupTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void SeedsAllFiveEmailOutboxStatusCodes()
    {
        using var context = _fixture.CreateContext();
        var codes = context.EmailOutboxStatuses.Select(s => s.Code).ToList();

        Assert.Equal(5, codes.Count);
        Assert.Contains("pending", codes);
        Assert.Contains("sending", codes);
        Assert.Contains("sent", codes);
        Assert.Contains("failed", codes);
        Assert.Contains("skipped", codes);
    }

    [Fact]
    public void SeedsBothEmailLogStatusCodes()
    {
        using var context = _fixture.CreateContext();
        var codes = context.EmailLogStatuses.Select(s => s.Code).ToList();

        Assert.Equal(2, codes.Count);
        Assert.Contains("sent", codes);
        Assert.Contains("failed", codes);
    }

    [Fact]
    public void RejectsDuplicateEmailOutboxStatusCode()
    {
        using var context = _fixture.CreateContext();
        context.EmailOutboxStatuses.Add(new EmailOutboxStatus { Id = Guid.NewGuid(), Code = "pending", NameAr = "مكرر", NameEn = "Dup", SortOrder = 99 });

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }
}
