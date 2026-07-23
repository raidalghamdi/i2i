using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class CmsBlockConfigurationTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public CmsBlockConfigurationTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void SavesPublishedBlockForAUniqueKey()
    {
        Guid blockId;

        using (var context = _fixture.CreateContext())
        {
            var block = new CmsBlock
            {
                Id = Guid.NewGuid(),
                Key = "homepage-hero-t3a",
                ContentAr = "مرحبا",
                ContentEn = "Welcome",
            };
            blockId = block.Id;

            context.CmsBlocks.Add(block);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var block = context.CmsBlocks.Single(b => b.Id == blockId);
            Assert.True(block.IsPublished);
            Assert.Equal("Welcome", block.ContentEn);
        }
    }

    [Fact]
    public void RejectsDuplicateBlockKey()
    {
        using var context = _fixture.CreateContext();

        context.CmsBlocks.Add(new CmsBlock { Id = Guid.NewGuid(), Key = "dup-key-t3b", ContentAr = "أ", ContentEn = "A" });
        context.SaveChanges();

        context.CmsBlocks.Add(new CmsBlock { Id = Guid.NewGuid(), Key = "dup-key-t3b", ContentAr = "ب", ContentEn = "B" });

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }
}
