using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Content;

namespace InnovationToImpact.Infrastructure.Tests;

public class PublicContentServiceTests
{
    private static void Seed(SqliteContextFixture fixture, string slug, bool published)
    {
        using var db = fixture.CreateContext();
        db.CmsContents.Add(new CmsContent
        {
            Id = Guid.NewGuid(), Slug = slug,
            TitleAr = "ع", TitleEn = "Title", BodyAr = "بدن", BodyEn = "Body",
            IsPublished = published,
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task GetPublishedBySlug_ReturnsPublishedRow()
    {
        using var fixture = new SqliteContextFixture();
        Seed(fixture, "about", published: true);
        using var db = fixture.CreateContext();
        var result = await new PublicContentService(db).GetPublishedBySlugAsync("about", default);
        Assert.NotNull(result);
        Assert.Equal("Title", result!.TitleEn);
    }

    [Fact]
    public async Task GetPublishedBySlug_UnpublishedReturnsNull()
    {
        using var fixture = new SqliteContextFixture();
        Seed(fixture, "about", published: false);
        using var db = fixture.CreateContext();
        var result = await new PublicContentService(db).GetPublishedBySlugAsync("about", default);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPublishedBySlug_MissingReturnsNull()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var result = await new PublicContentService(db).GetPublishedBySlugAsync("nope", default);
        Assert.Null(result);
    }
}
