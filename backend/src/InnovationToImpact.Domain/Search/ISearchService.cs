namespace InnovationToImpact.Domain.Search;

public sealed record SearchResultItem(string Type, Guid Id, string TitleEn, string TitleAr, string Subtitle, string Link);
public sealed record SearchResults(IReadOnlyList<SearchResultItem> Ideas, IReadOnlyList<SearchResultItem> Challenges, IReadOnlyList<SearchResultItem> Users);

public interface ISearchService
{
    Task<SearchResults> SearchAsync(string q, Guid userId, string userEmail, IReadOnlyCollection<string> roles, CancellationToken cancellationToken = default);
}
