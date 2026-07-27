using System.Text.Json;
using InnovationToImpact.Domain.Audit;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Sla;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Sla;

// Change 20260726
public class SlaPolicyService : ISlaPolicyService
{
    private readonly InnovationDbContext _db;
    private readonly IAuditLogWriter _auditLogWriter;

    public SlaPolicyService(InnovationDbContext db, IAuditLogWriter auditLogWriter)
    {
        _db = db;
        _auditLogWriter = auditLogWriter;
    }

    public async Task<IReadOnlyList<SlaPolicy>> ListAllAsync(CancellationToken cancellationToken = default) =>
        await _db.SlaPolicies
            .OrderBy(p => p.EntityType)
            .ThenBy(p => p.FromState)
            .ToListAsync(cancellationToken);

    public async Task<SlaPolicy?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _db.SlaPolicies.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<SlaPolicyCommandResult> CreateAsync(SlaPolicyInput input, Guid actorId, CancellationToken cancellationToken = default)
    {
        if (!IsValid(input)) return new SlaPolicyCommandResult(SlaPolicyCommandStatus.Invalid, null);

        if (await TransitionExistsAsync(input, null, cancellationToken))
            return new SlaPolicyCommandResult(SlaPolicyCommandStatus.DuplicateTransition, null);

        var policy = new SlaPolicy
        {
            Id = Guid.NewGuid(),
            EntityType = input.EntityType,
            FromState = input.FromState,
            ToState = input.ToState,
            TargetHours = input.TargetHours,
            WarnAtPct = input.WarnAtPct,
        };
        _db.SlaPolicies.Add(policy);
        await _db.SaveChangesAsync(cancellationToken);
        await _auditLogWriter.AppendAsync("sla_policy", policy.Id, "sla_policy.created", actorId, JsonSerializer.Serialize(input), cancellationToken);

        return new SlaPolicyCommandResult(SlaPolicyCommandStatus.Success, policy);
    }

    public async Task<SlaPolicyCommandResult> UpdateAsync(Guid id, SlaPolicyInput input, Guid actorId, CancellationToken cancellationToken = default)
    {
        if (!IsValid(input)) return new SlaPolicyCommandResult(SlaPolicyCommandStatus.Invalid, null);

        var policy = await _db.SlaPolicies.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (policy is null) return new SlaPolicyCommandResult(SlaPolicyCommandStatus.NotFound, null);

        if (await TransitionExistsAsync(input, id, cancellationToken))
            return new SlaPolicyCommandResult(SlaPolicyCommandStatus.DuplicateTransition, null);

        policy.EntityType = input.EntityType;
        policy.FromState = input.FromState;
        policy.ToState = input.ToState;
        policy.TargetHours = input.TargetHours;
        policy.WarnAtPct = input.WarnAtPct;
        await _db.SaveChangesAsync(cancellationToken);
        await _auditLogWriter.AppendAsync("sla_policy", policy.Id, "sla_policy.updated", actorId, JsonSerializer.Serialize(input), cancellationToken);

        return new SlaPolicyCommandResult(SlaPolicyCommandStatus.Success, policy);
    }

    public async Task<SlaPolicyCommandResult> DeleteAsync(Guid id, Guid actorId, CancellationToken cancellationToken = default)
    {
        var policy = await _db.SlaPolicies.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (policy is null) return new SlaPolicyCommandResult(SlaPolicyCommandStatus.NotFound, null);

        _db.SlaPolicies.Remove(policy);
        await _db.SaveChangesAsync(cancellationToken);
        await _auditLogWriter.AppendAsync("sla_policy", policy.Id, "sla_policy.deleted", actorId, null, cancellationToken);

        return new SlaPolicyCommandResult(SlaPolicyCommandStatus.Success, policy);
    }

    private static bool IsValid(SlaPolicyInput input) =>
        !string.IsNullOrWhiteSpace(input.EntityType)
        && !string.IsNullOrWhiteSpace(input.FromState)
        && !string.IsNullOrWhiteSpace(input.ToState)
        && input.TargetHours > 0
        && input.WarnAtPct is > 0 and <= 100;

    private Task<bool> TransitionExistsAsync(SlaPolicyInput input, Guid? excludeId, CancellationToken cancellationToken) =>
        _db.SlaPolicies.AnyAsync(
            p => (excludeId == null || p.Id != excludeId)
                && p.EntityType == input.EntityType
                && p.FromState == input.FromState
                && p.ToState == input.ToState,
            cancellationToken);
}
