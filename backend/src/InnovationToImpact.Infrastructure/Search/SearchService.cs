using InnovationToImpact.Domain.Auth;
using InnovationToImpact.Domain.Ideas;
using InnovationToImpact.Domain.Search;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Search;

public sealed class SearchService : ISearchService
{
    private readonly InnovationDbContext _db;
    private readonly IIdeaService _ideaService;

    public SearchService(InnovationDbContext db, IIdeaService ideaService)
    {
        _db = db;
        _ideaService = ideaService;
    }

    public async Task<SearchResults> SearchAsync(string q, Guid userId, string userEmail, IReadOnlyCollection<string> roles, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return new SearchResults(
                Array.Empty<SearchResultItem>(),
                Array.Empty<SearchResultItem>(),
                Array.Empty<SearchResultItem>());
        }

        var page = await _ideaService.ListAsync(new IdeaListFilter(q, null, null, null, null, 1, 5), userId, userEmail, roles, cancellationToken);
        var ideas = page.Items
            .Select(i => new SearchResultItem("idea", i.Id, i.TitleEn, i.TitleAr, i.Code, $"/ideas/{i.Id}"))
            .ToList();

        IReadOnlyList<SearchResultItem> challenges = Array.Empty<SearchResultItem>();
        IReadOnlyList<SearchResultItem> users = Array.Empty<SearchResultItem>();

        if (roles.Contains(RoleCodes.Admin))
        {
            challenges = await _db.Challenges
                .Where(c => c.TextEn.Contains(q) || c.TextAr.Contains(q))
                .OrderBy(c => c.SortOrder)
                .Take(5)
                .Select(c => new SearchResultItem("challenge", c.Id, c.TextEn, c.TextAr, "", $"/admin/challenges/{c.Id}/edit"))
                .ToListAsync(cancellationToken);

            users = await _db.Users
                .Where(u => u.FullNameEn.Contains(q) || u.FullNameAr.Contains(q) || u.Email.Contains(q) || u.SamAccountName.Contains(q))
                .Take(5)
                .Select(u => new SearchResultItem("user", u.Id, u.FullNameEn, u.FullNameAr, u.Email, $"/admin/users/{u.Id}"))
                .ToListAsync(cancellationToken);
        }

        return new SearchResults(ideas, challenges, users);
    }
}
