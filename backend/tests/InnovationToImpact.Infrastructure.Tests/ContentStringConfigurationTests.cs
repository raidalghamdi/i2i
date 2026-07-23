using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class ContentStringConfigurationTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public ContentStringConfigurationTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void SavesStringOverrideForAUniqueKey()
    {
        Guid stringId;

        using (var context = _fixture.CreateContext())
        {
            var contentString = new ContentString
            {
                Id = Guid.NewGuid(),
                Key = "homepage.hero.subtitle-t2a",
                ValueAr = "مرحبا بكم",
                ValueEn = "Welcome",
            };
            stringId = contentString.Id;

            context.ContentStrings.Add(contentString);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var contentString = context.ContentStrings.Single(s => s.Id == stringId);
            Assert.Equal("Welcome", contentString.ValueEn);
        }
    }

    [Fact]
    public void RejectsDuplicateKey()
    {
        using var context = _fixture.CreateContext();

        context.ContentStrings.Add(new ContentString { Id = Guid.NewGuid(), Key = "dup-key-t2b", ValueAr = "أ", ValueEn = "A" });
        context.SaveChanges();

        context.ContentStrings.Add(new ContentString { Id = Guid.NewGuid(), Key = "dup-key-t2b", ValueAr = "ب", ValueEn = "B" });

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }
}
