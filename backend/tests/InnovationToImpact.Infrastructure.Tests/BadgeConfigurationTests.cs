using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class BadgeConfigurationTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public BadgeConfigurationTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void SavesActiveBadgeWithNullDescription()
    {
        Guid badgeId;

        using (var context = _fixture.CreateContext())
        {
            var badge = new Badge
            {
                Id = Guid.NewGuid(),
                Code = "first-idea-t1a",
                NameAr = "أول فكرة",
                NameEn = "First Idea",
            };
            badgeId = badge.Id;

            context.Badges.Add(badge);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var badge = context.Badges.Single(b => b.Id == badgeId);
            Assert.True(badge.IsActive);
            Assert.Null(badge.DescriptionAr);
            Assert.Null(badge.DescriptionEn);
        }
    }

    [Fact]
    public void RejectsDuplicateBadgeCode()
    {
        using var context = _fixture.CreateContext();

        context.Badges.Add(new Badge { Id = Guid.NewGuid(), Code = "dup-code-t1b", NameAr = "أ", NameEn = "A" });
        context.SaveChanges();

        context.Badges.Add(new Badge { Id = Guid.NewGuid(), Code = "dup-code-t1b", NameAr = "ب", NameEn = "B" });

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }
}
