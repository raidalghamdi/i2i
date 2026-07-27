using System.Text.Json;
using InnovationToImpact.Domain.Audit;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Evaluations;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Evaluations;

// Change 20260726
public class EvaluationCriteriaService : IEvaluationCriteriaService
{
    private readonly InnovationDbContext _db;
    private readonly IAuditLogWriter _auditLogWriter;

    public EvaluationCriteriaService(InnovationDbContext db, IAuditLogWriter auditLogWriter)
    {
        _db = db;
        _auditLogWriter = auditLogWriter;
    }

    public async Task<IReadOnlyList<EvaluationCriterion>> ListActiveAsync(CancellationToken cancellationToken = default) =>
        await _db.EvaluationCriteria
            .Where(c => c.Active)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Code)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<EvaluationCriterion>> ListAllAsync(CancellationToken cancellationToken = default) =>
        await _db.EvaluationCriteria
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Code)
            .ToListAsync(cancellationToken);

    public async Task<EvaluationCriteriaCommandResult> CreateAsync(EvaluationCriterionInput input, Guid actorId, CancellationToken cancellationToken = default)
    {
        var duplicate = await _db.EvaluationCriteria.AnyAsync(c => c.Code == input.Code, cancellationToken);
        if (duplicate) return new EvaluationCriteriaCommandResult(EvaluationCriteriaCommandStatus.DuplicateCode, null);

        var criterion = new EvaluationCriterion
        {
            Id = Guid.NewGuid(),
            Code = input.Code,
            NameAr = input.NameAr,
            NameEn = input.NameEn,
            DescriptionAr = input.DescriptionAr,
            DescriptionEn = input.DescriptionEn,
            Weight = input.Weight,
            Active = input.Active,
            SortOrder = input.SortOrder,
        };
        _db.EvaluationCriteria.Add(criterion);
        await _db.SaveChangesAsync(cancellationToken);
        await _auditLogWriter.AppendAsync("evaluation_criterion", criterion.Id, "evaluation_criterion.created", actorId, JsonSerializer.Serialize(input), cancellationToken);

        return new EvaluationCriteriaCommandResult(EvaluationCriteriaCommandStatus.Success, criterion);
    }

    public async Task<EvaluationCriteriaCommandResult> UpdateAsync(Guid id, EvaluationCriterionInput input, Guid actorId, CancellationToken cancellationToken = default)
    {
        var criterion = await _db.EvaluationCriteria.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (criterion is null) return new EvaluationCriteriaCommandResult(EvaluationCriteriaCommandStatus.NotFound, null);

        var duplicate = await _db.EvaluationCriteria.AnyAsync(c => c.Id != id && c.Code == input.Code, cancellationToken);
        if (duplicate) return new EvaluationCriteriaCommandResult(EvaluationCriteriaCommandStatus.DuplicateCode, null);

        criterion.Code = input.Code;
        criterion.NameAr = input.NameAr;
        criterion.NameEn = input.NameEn;
        criterion.DescriptionAr = input.DescriptionAr;
        criterion.DescriptionEn = input.DescriptionEn;
        criterion.Weight = input.Weight;
        criterion.Active = input.Active;
        criterion.SortOrder = input.SortOrder;
        await _db.SaveChangesAsync(cancellationToken);
        await _auditLogWriter.AppendAsync("evaluation_criterion", criterion.Id, "evaluation_criterion.updated", actorId, JsonSerializer.Serialize(input), cancellationToken);

        return new EvaluationCriteriaCommandResult(EvaluationCriteriaCommandStatus.Success, criterion);
    }

    public async Task<EvaluationCriteriaCommandResult> DeleteAsync(Guid id, Guid actorId, CancellationToken cancellationToken = default)
    {
        var criterion = await _db.EvaluationCriteria.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (criterion is null) return new EvaluationCriteriaCommandResult(EvaluationCriteriaCommandStatus.NotFound, null);

        _db.EvaluationCriteria.Remove(criterion);
        await _db.SaveChangesAsync(cancellationToken);
        await _auditLogWriter.AppendAsync("evaluation_criterion", criterion.Id, "evaluation_criterion.deleted", actorId, null, cancellationToken);

        return new EvaluationCriteriaCommandResult(EvaluationCriteriaCommandStatus.Success, criterion);
    }
}
