using System.Text.Json;
using InnovationToImpact.Domain.Audit;
using InnovationToImpact.Domain.Committee;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Committee;

public class CommitteeCriteriaService : ICommitteeCriteriaService
{
    private readonly InnovationDbContext _db;
    private readonly IAuditLogWriter _auditLogWriter;

    public CommitteeCriteriaService(InnovationDbContext db, IAuditLogWriter auditLogWriter)
    {
        _db = db;
        _auditLogWriter = auditLogWriter;
    }

    public async Task<IReadOnlyList<CommitteeCriterion>> ListAllAsync(CancellationToken cancellationToken = default) =>
        await _db.CommitteeCriteria
            .OrderBy(c => c.Code)
            .ToListAsync(cancellationToken);

    public async Task<CommitteeCriteriaCommandResult> CreateAsync(CommitteeCriterionInput input, Guid actorId, CancellationToken cancellationToken = default)
    {
        var duplicate = await _db.CommitteeCriteria.AnyAsync(c => c.Code == input.Code, cancellationToken);
        if (duplicate) return new CommitteeCriteriaCommandResult(CommitteeCriteriaCommandStatus.DuplicateCode, null);

        var criterion = new CommitteeCriterion
        {
            Id = Guid.NewGuid(),
            Code = input.Code,
            NameAr = input.NameAr,
            NameEn = input.NameEn,
            DescriptionAr = input.DescriptionAr,
            DescriptionEn = input.DescriptionEn,
            Weight = input.Weight,
            Active = input.Active,
        };
        _db.CommitteeCriteria.Add(criterion);
        await _db.SaveChangesAsync(cancellationToken);
        await _auditLogWriter.AppendAsync("committee_criterion", criterion.Id, "committee_criterion.created", actorId, JsonSerializer.Serialize(input), cancellationToken);

        return new CommitteeCriteriaCommandResult(CommitteeCriteriaCommandStatus.Success, criterion);
    }

    public async Task<CommitteeCriteriaCommandResult> UpdateAsync(Guid id, CommitteeCriterionInput input, Guid actorId, CancellationToken cancellationToken = default)
    {
        var criterion = await _db.CommitteeCriteria.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (criterion is null) return new CommitteeCriteriaCommandResult(CommitteeCriteriaCommandStatus.NotFound, null);

        var duplicate = await _db.CommitteeCriteria.AnyAsync(c => c.Id != id && c.Code == input.Code, cancellationToken);
        if (duplicate) return new CommitteeCriteriaCommandResult(CommitteeCriteriaCommandStatus.DuplicateCode, null);

        criterion.Code = input.Code;
        criterion.NameAr = input.NameAr;
        criterion.NameEn = input.NameEn;
        criterion.DescriptionAr = input.DescriptionAr;
        criterion.DescriptionEn = input.DescriptionEn;
        criterion.Weight = input.Weight;
        criterion.Active = input.Active;
        await _db.SaveChangesAsync(cancellationToken);
        await _auditLogWriter.AppendAsync("committee_criterion", criterion.Id, "committee_criterion.updated", actorId, JsonSerializer.Serialize(input), cancellationToken);

        return new CommitteeCriteriaCommandResult(CommitteeCriteriaCommandStatus.Success, criterion);
    }

    public async Task<CommitteeCriteriaCommandResult> DeleteAsync(Guid id, Guid actorId, CancellationToken cancellationToken = default)
    {
        var criterion = await _db.CommitteeCriteria.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (criterion is null) return new CommitteeCriteriaCommandResult(CommitteeCriteriaCommandStatus.NotFound, null);

        _db.CommitteeCriteria.Remove(criterion);
        await _db.SaveChangesAsync(cancellationToken);
        await _auditLogWriter.AppendAsync("committee_criterion", criterion.Id, "committee_criterion.deleted", actorId, null, cancellationToken);

        return new CommitteeCriteriaCommandResult(CommitteeCriteriaCommandStatus.Success, criterion);
    }
}
