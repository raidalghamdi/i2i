using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.Home;

public interface IHomePageService
{
    /// <summary>Visible sections only, ordered by Idx. For the anonymous public endpoint.</summary>
    Task<IReadOnlyList<HomePageSection>> ListPublicAsync(CancellationToken cancellationToken = default);

    /// <summary>All sections (visible + hidden), ordered by Idx. For the admin endpoint.</summary>
    Task<IReadOnlyList<HomePageSection>> ListAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the whole ordered set: existing rows (matched by Id) are updated in place, rows with
    /// no Id are inserted as new, and any existing row not present in the input is deleted.
    /// </summary>
    Task ReplaceAllAsync(IReadOnlyList<HomePageSectionInput> sections, Guid actorId, CancellationToken cancellationToken = default);

    Task<HomePageSection> AddAsync(HomePageSectionInput input, Guid actorId, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed record HomePageSectionInput(Guid? Id, int Idx, string Type, bool IsVisible, string ContentJson);
