namespace InnovationToImpact.Domain.Content;

public sealed record PublicTrack(Guid Id, string NameAr, string NameEn, string? DescriptionAr, string? DescriptionEn, int Priority);
public sealed record PublicIdeaSummary(Guid Id, string Code, string TitleAr, string TitleEn, string Status);
public sealed record PublicTrackDetail(PublicTrack Track, IReadOnlyList<PublicChallenge> Challenges, IReadOnlyList<PublicIdeaSummary> Ideas);
public sealed record PublicChallenge(Guid Id, string TextAr, string TextEn);
public sealed record PublicActivity(Guid Id, string NameAr, string NameEn, string Type, string Status, DateTime StartDate, DateTime EndDate, int IdeaCount);
public sealed record PublicActivityDetail(PublicActivity Activity, int ApprovedCount, int PilotingCount, IReadOnlyList<PublicIdeaSummary> Ideas);
public sealed record PublicSearchResults(IReadOnlyList<PublicIdeaSummary> Ideas, IReadOnlyList<PublicTrack> Tracks);

public interface IPublicDataService
{
    Task<IReadOnlyList<PublicTrack>> ListTracksAsync(CancellationToken ct = default);
    Task<PublicTrackDetail?> GetTrackAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<PublicActivity>> ListActivitiesAsync(CancellationToken ct = default);
    Task<PublicActivityDetail?> GetActivityAsync(Guid id, CancellationToken ct = default);
    Task<PublicSearchResults> SearchAsync(string query, CancellationToken ct = default);
}
