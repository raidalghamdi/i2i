using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class CmsContentConfigurationTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public CmsContentConfigurationTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void SavesDraftContentWithNullPublishedAt()
    {
        Guid contentId;

        using (var context = _fixture.CreateContext())
        {
            var content = new CmsContent
            {
                Id = Guid.NewGuid(),
                Slug = "about-us-t1a",
                TitleAr = "من نحن",
                TitleEn = "About Us",
                BodyAr = "محتوى",
                BodyEn = "Content",
                PublishedAt = null,
            };
            contentId = content.Id;

            context.CmsContents.Add(content);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var content = context.CmsContents.Single(c => c.Id == contentId);
            Assert.True(content.IsPublished);
            Assert.Null(content.PublishedAt);
        }
    }

    [Fact]
    public void RejectsDuplicateSlug()
    {
        using var context = _fixture.CreateContext();

        context.CmsContents.Add(new CmsContent { Id = Guid.NewGuid(), Slug = "dup-slug-t1b", TitleAr = "أ", TitleEn = "A", BodyAr = "أ", BodyEn = "A" });
        context.SaveChanges();

        context.CmsContents.Add(new CmsContent { Id = Guid.NewGuid(), Slug = "dup-slug-t1b", TitleAr = "ب", TitleEn = "B", BodyAr = "ب", BodyEn = "B" });

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }
}
