using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class KnowledgeArticleConfigurationTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public KnowledgeArticleConfigurationTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    private static (Guid ideaId, Guid authorId, Guid articleTypeId, Guid publicVisibilityId) SeedPrerequisites(InnovationDbContext context, string suffix)
    {
        var submitterId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var themeId = Guid.NewGuid();

        context.Users.Add(new User { Id = submitterId, SamAccountName = $"submitter-{suffix}", Email = $"sub-{suffix}@gac-demo.sa", FullNameAr = "أ", FullNameEn = "Submitter" });
        context.Users.Add(new User { Id = authorId, SamAccountName = $"author-{suffix}", Email = $"author-{suffix}@gac-demo.sa", FullNameAr = "ب", FullNameEn = "Author" });
        context.StrategicThemes.Add(new StrategicTheme { Id = themeId, NameAr = "محور", NameEn = "Theme", OwnerId = submitterId });
        context.SaveChanges();

        var ideaStatusId = context.IdeaStatuses.Single(s => s.Code == "draft").Id;
        var ideaId = Guid.NewGuid();
        context.Ideas.Add(new Idea
        {
            Id = ideaId,
            Code = $"IDEA-{suffix}",
            TitleAr = "فكرة", TitleEn = "Idea",
            ProblemStatementAr = "أ", ProblemStatementEn = "A",
            ProposedSolutionAr = "أ", ProposedSolutionEn = "A",
            ExpectedBenefitsAr = "أ", ExpectedBenefitsEn = "A",
            StrategicThemeId = themeId,
            IdeaStatusId = ideaStatusId,
            CurrentStage = 8,
            SubmitterId = submitterId,
        });
        context.SaveChanges();

        var articleTypeId = context.KnowledgeTypes.Single(t => t.Code == "article").Id;
        var publicVisibilityId = context.KnowledgeVisibilities.Single(v => v.Code == "public").Id;
        return (ideaId, authorId, articleTypeId, publicVisibilityId);
    }

    [Fact]
    public void SavesKnowledgeArticleLinkedToAnIdea()
    {
        Guid articleId;

        using (var context = _fixture.CreateContext())
        {
            var (ideaId, authorId, articleTypeId, publicVisibilityId) = SeedPrerequisites(context, "kb-t4a");

            var article = new KnowledgeArticle
            {
                Id = Guid.NewGuid(),
                IdeaId = ideaId,
                TitleAr = "مقال",
                TitleEn = "Article",
                KnowledgeTypeId = articleTypeId,
                ContentMdAr = "محتوى",
                ContentMdEn = "Content",
                KnowledgeVisibilityId = publicVisibilityId,
                AuthorId = authorId,
            };
            articleId = article.Id;

            context.KnowledgeArticles.Add(article);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var article = context.KnowledgeArticles
                .Include(a => a.KnowledgeType)
                .Include(a => a.KnowledgeVisibility)
                .Single(a => a.Id == articleId);
            Assert.Equal("article", article.KnowledgeType.Code);
            Assert.Equal("public", article.KnowledgeVisibility.Code);
        }
    }

    [Fact]
    public void AllowsNullIdeaIdForAStandaloneArticle()
    {
        using var context = _fixture.CreateContext();
        var (_, authorId, articleTypeId, publicVisibilityId) = SeedPrerequisites(context, "kb-t4b");

        context.KnowledgeArticles.Add(new KnowledgeArticle
        {
            Id = Guid.NewGuid(),
            IdeaId = null,
            TitleAr = "أ", TitleEn = "A",
            KnowledgeTypeId = articleTypeId,
            ContentMdAr = "أ", ContentMdEn = "A",
            KnowledgeVisibilityId = publicVisibilityId,
            AuthorId = authorId,
        });

        var exception = Record.Exception(() => context.SaveChanges());
        Assert.Null(exception);
    }

    [Fact]
    public void DeletingIdeaSetsKnowledgeArticleIdeaIdToNull()
    {
        Guid articleId;
        Guid ideaId;

        using (var context = _fixture.CreateContext())
        {
            var (seededIdeaId, authorId, articleTypeId, publicVisibilityId) = SeedPrerequisites(context, "kb-t4c");
            ideaId = seededIdeaId;

            var article = new KnowledgeArticle
            {
                Id = Guid.NewGuid(),
                IdeaId = ideaId,
                TitleAr = "مقال", TitleEn = "Article",
                KnowledgeTypeId = articleTypeId,
                ContentMdAr = "أ", ContentMdEn = "A",
                KnowledgeVisibilityId = publicVisibilityId,
                AuthorId = authorId,
            };
            articleId = article.Id;

            context.KnowledgeArticles.Add(article);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var idea = context.Ideas.Single(i => i.Id == ideaId);
            context.Ideas.Remove(idea);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var article = context.KnowledgeArticles.Single(a => a.Id == articleId);
            Assert.Null(article.IdeaId);
        }
    }
}
