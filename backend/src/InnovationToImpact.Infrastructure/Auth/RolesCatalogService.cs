using System.Text.Json;
using InnovationToImpact.Domain.Audit;
using InnovationToImpact.Domain.Auth;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Auth;

public class RolesCatalogService : IRolesCatalogService
{
    private readonly InnovationDbContext _db;
    private readonly IAuditLogWriter _auditLogWriter;

    public RolesCatalogService(InnovationDbContext db, IAuditLogWriter auditLogWriter)
    {
        _db = db;
        _auditLogWriter = auditLogWriter;
    }

    public async Task<IReadOnlyList<RoleCatalogRow>> ListAllAsync(CancellationToken cancellationToken = default) =>
        await _db.Roles
            .OrderBy(r => r.SortOrder)
            .Select(r => new RoleCatalogRow(
                r.Id,
                r.Code,
                r.NameAr,
                r.NameEn,
                r.DescriptionAr,
                r.DescriptionEn,
                r.IsSystem,
                r.IsActive,
                r.SortOrder))
            .ToListAsync(cancellationToken);

    public async Task<RolePatchResult> PatchAsync(Guid id, RoleCatalogPatch patch, Guid actorId, CancellationToken cancellationToken = default)
    {
        var role = await _db.Roles.SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (role is null)
            return new RolePatchResult(RolePatchStatus.NotFound, null);

        if (patch.NameAr is not null) role.NameAr = patch.NameAr;
        if (patch.NameEn is not null) role.NameEn = patch.NameEn;
        if (patch.DescriptionAr is not null) role.DescriptionAr = patch.DescriptionAr;
        if (patch.DescriptionEn is not null) role.DescriptionEn = patch.DescriptionEn;
        if (patch.IsActive is not null) role.IsActive = patch.IsActive.Value;
        if (patch.SortOrder is not null) role.SortOrder = patch.SortOrder.Value;

        await _db.SaveChangesAsync(cancellationToken);

        await _auditLogWriter.AppendAsync(
            "role",
            role.Id,
            "role.updated",
            actorId,
            JsonSerializer.Serialize(patch),
            cancellationToken);

        var row = new RoleCatalogRow(
            role.Id,
            role.Code,
            role.NameAr,
            role.NameEn,
            role.DescriptionAr,
            role.DescriptionEn,
            role.IsSystem,
            role.IsActive,
            role.SortOrder);

        return new RolePatchResult(RolePatchStatus.Success, row);
    }
}
