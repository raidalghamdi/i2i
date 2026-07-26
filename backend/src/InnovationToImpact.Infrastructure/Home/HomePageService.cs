using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Home;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Home;

public class HomePageService : IHomePageService
{
    private readonly InnovationDbContext _db;

    public HomePageService(InnovationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<HomePageSection>> ListPublicAsync(CancellationToken cancellationToken = default) =>
        await _db.HomePageSections
            .Where(s => s.IsVisible)
            .OrderBy(s => s.Idx)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<HomePageSection>> ListAllAsync(CancellationToken cancellationToken = default) =>
        await _db.HomePageSections
            .OrderBy(s => s.Idx)
            .ToListAsync(cancellationToken);

    public async Task ReplaceAllAsync(IReadOnlyList<HomePageSectionInput> sections, Guid actorId, CancellationToken cancellationToken = default)
    {
        var existing = await _db.HomePageSections.ToDictionaryAsync(s => s.Id, cancellationToken);
        var keepIds = new HashSet<Guid>();
        var now = DateTime.UtcNow;

        foreach (var input in sections)
        {
            if (input.Id is Guid id && existing.TryGetValue(id, out var section))
            {
                section.Idx = input.Idx;
                section.Type = input.Type;
                section.IsVisible = input.IsVisible;
                section.ContentJson = input.ContentJson;
                section.UpdatedAt = now;
                section.UpdatedById = actorId;
                keepIds.Add(id);
            }
            else
            {
                var newSection = new HomePageSection
                {
                    Id = Guid.NewGuid(),
                    Idx = input.Idx,
                    Type = input.Type,
                    IsVisible = input.IsVisible,
                    ContentJson = input.ContentJson,
                    UpdatedAt = now,
                    UpdatedById = actorId,
                };
                _db.HomePageSections.Add(newSection);
                keepIds.Add(newSection.Id);
            }
        }

        foreach (var section in existing.Values)
        {
            if (!keepIds.Contains(section.Id))
            {
                _db.HomePageSections.Remove(section);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<HomePageSection> AddAsync(HomePageSectionInput input, Guid actorId, CancellationToken cancellationToken = default)
    {
        var section = new HomePageSection
        {
            Id = Guid.NewGuid(),
            Idx = input.Idx,
            Type = input.Type,
            IsVisible = input.IsVisible,
            ContentJson = input.ContentJson,
            UpdatedAt = DateTime.UtcNow,
            UpdatedById = actorId,
        };
        _db.HomePageSections.Add(section);
        await _db.SaveChangesAsync(cancellationToken);
        return section;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var section = await _db.HomePageSections.SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (section is null) return;

        _db.HomePageSections.Remove(section);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
