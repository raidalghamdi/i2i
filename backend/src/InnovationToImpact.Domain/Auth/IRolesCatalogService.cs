namespace InnovationToImpact.Domain.Auth;

public sealed record RoleCatalogRow(
    Guid Id,
    string Code,
    string NameAr,
    string NameEn,
    string? DescriptionAr,
    string? DescriptionEn,
    bool IsSystem,
    bool IsActive,
    int SortOrder);

public sealed record RoleCatalogPatch(
    string? NameAr,
    string? NameEn,
    string? DescriptionAr,
    string? DescriptionEn,
    bool? IsActive,
    int? SortOrder);

public enum RolePatchStatus
{
    Success,
    NotFound,
}

public sealed record RolePatchResult(RolePatchStatus Status, RoleCatalogRow? Role);

public interface IRolesCatalogService
{
    Task<IReadOnlyList<RoleCatalogRow>> ListAllAsync(CancellationToken cancellationToken = default);

    Task<RolePatchResult> PatchAsync(Guid id, RoleCatalogPatch patch, Guid actorId, CancellationToken cancellationToken = default);
}
