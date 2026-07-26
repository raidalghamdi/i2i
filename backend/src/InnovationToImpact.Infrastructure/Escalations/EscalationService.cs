using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Escalations;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Escalations;

public class EscalationService : IEscalationService
{
    private const string OpenStatus = "open";
    private const string ResolvedStatus = "resolved";

    private readonly InnovationDbContext _db;

    public EscalationService(InnovationDbContext db)
    {
        _db = db;
    }

    public async Task<Escalation> OpenIfAbsentAsync(string entityType, Guid entityId, string reasonAr, string reasonEn, CancellationToken cancellationToken = default)
    {
        var existing = await _db.Escalations
            .Include(e => e.EscalationTier)
            .Include(e => e.EscalationStatus)
            .Where(e => e.EntityType == entityType && e.EntityId == entityId && e.EscalationStatus.Code != ResolvedStatus)
            .OrderByDescending(e => e.OpenedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (existing is not null) return existing;

        var tier1 = await _db.EscalationTiers.OrderBy(t => t.SortOrder).FirstAsync(cancellationToken);
        var openStatus = await _db.EscalationStatuses.SingleAsync(s => s.Code == OpenStatus, cancellationToken);
        var owner = await ResolveOwnerAsync(tier1.Id, cancellationToken);

        var escalation = new Escalation
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            EscalationTierId = tier1.Id,
            EscalationTier = tier1,
            ReasonAr = reasonAr,
            ReasonEn = reasonEn,
            EscalationStatusId = openStatus.Id,
            EscalationStatus = openStatus,
            OwnerId = owner?.Id,
            OpenedAt = DateTime.UtcNow,
        };
        _db.Escalations.Add(escalation);

        _db.EscalationEvents.Add(new EscalationEvent
        {
            Id = Guid.NewGuid(),
            EscalationId = escalation.Id,
            EventType = "opened",
            ToTierId = tier1.Id,
            CreatedAt = DateTime.UtcNow,
        });

        await _db.SaveChangesAsync(cancellationToken);
        return escalation;
    }

    public async Task<EscalationCommandResult> AcknowledgeAsync(Guid escalationId, Guid actorId, string? notes, CancellationToken cancellationToken = default)
    {
        var escalation = await _db.Escalations.Include(e => e.EscalationStatus).Include(e => e.EscalationTier).Include(e => e.Owner).SingleOrDefaultAsync(e => e.Id == escalationId, cancellationToken);
        if (escalation is null) return new EscalationCommandResult(EscalationCommandStatus.NotFound);
        if (escalation.EscalationStatus.Code != OpenStatus) return new EscalationCommandResult(EscalationCommandStatus.InvalidStatusForAction);

        var ackStatus = await _db.EscalationStatuses.SingleAsync(s => s.Code == "acknowledged", cancellationToken);
        escalation.EscalationStatusId = ackStatus.Id;
        escalation.EscalationStatus = ackStatus;

        _db.EscalationEvents.Add(new EscalationEvent
        {
            Id = Guid.NewGuid(),
            EscalationId = escalation.Id,
            EventType = "ack",
            ActorId = actorId,
            Notes = notes,
            CreatedAt = DateTime.UtcNow,
        });

        await _db.SaveChangesAsync(cancellationToken);
        return new EscalationCommandResult(EscalationCommandStatus.Success, escalation);
    }

    public async Task<EscalationCommandResult> BumpAsync(Guid escalationId, Guid actorId, string? notes, CancellationToken cancellationToken = default)
    {
        var escalation = await _db.Escalations.Include(e => e.EscalationStatus).Include(e => e.EscalationTier).Include(e => e.Owner).SingleOrDefaultAsync(e => e.Id == escalationId, cancellationToken);
        if (escalation is null) return new EscalationCommandResult(EscalationCommandStatus.NotFound);
        if (escalation.EscalationStatus.Code == ResolvedStatus) return new EscalationCommandResult(EscalationCommandStatus.InvalidStatusForAction);

        var nextTier = await _db.EscalationTiers.Where(t => t.SortOrder > escalation.EscalationTier.SortOrder).OrderBy(t => t.SortOrder).FirstOrDefaultAsync(cancellationToken);
        if (nextTier is null) return new EscalationCommandResult(EscalationCommandStatus.AlreadyMaxTier);

        var fromTierId = escalation.EscalationTierId;
        var owner = await ResolveOwnerAsync(nextTier.Id, cancellationToken);
        var openStatus = await _db.EscalationStatuses.SingleAsync(s => s.Code == OpenStatus, cancellationToken);

        escalation.EscalationTierId = nextTier.Id;
        escalation.EscalationTier = nextTier;
        escalation.OwnerId = owner?.Id;
        escalation.EscalationStatusId = openStatus.Id;
        escalation.EscalationStatus = openStatus;

        _db.EscalationEvents.Add(new EscalationEvent
        {
            Id = Guid.NewGuid(),
            EscalationId = escalation.Id,
            EventType = "bumped",
            FromTierId = fromTierId,
            ToTierId = nextTier.Id,
            ActorId = actorId,
            Notes = notes,
            CreatedAt = DateTime.UtcNow,
        });

        await _db.SaveChangesAsync(cancellationToken);
        return new EscalationCommandResult(EscalationCommandStatus.Success, escalation);
    }

    public async Task<EscalationCommandResult> ResolveAsync(Guid escalationId, Guid actorId, string resolutionAr, string resolutionEn, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(resolutionAr) || string.IsNullOrWhiteSpace(resolutionEn))
        {
            return new EscalationCommandResult(EscalationCommandStatus.ResolutionRequired);
        }

        var escalation = await _db.Escalations.Include(e => e.EscalationStatus).Include(e => e.EscalationTier).Include(e => e.Owner).SingleOrDefaultAsync(e => e.Id == escalationId, cancellationToken);
        if (escalation is null) return new EscalationCommandResult(EscalationCommandStatus.NotFound);
        if (escalation.EscalationStatus.Code == ResolvedStatus) return new EscalationCommandResult(EscalationCommandStatus.InvalidStatusForAction);

        var resolvedStatus = await _db.EscalationStatuses.SingleAsync(s => s.Code == ResolvedStatus, cancellationToken);
        escalation.EscalationStatusId = resolvedStatus.Id;
        escalation.EscalationStatus = resolvedStatus;
        escalation.ResolutionAr = resolutionAr;
        escalation.ResolutionEn = resolutionEn;

        _db.EscalationEvents.Add(new EscalationEvent
        {
            Id = Guid.NewGuid(),
            EscalationId = escalation.Id,
            EventType = "resolved",
            ActorId = actorId,
            CreatedAt = DateTime.UtcNow,
        });

        await _db.SaveChangesAsync(cancellationToken);
        return new EscalationCommandResult(EscalationCommandStatus.Success, escalation);
    }

    public async Task<IReadOnlyList<Escalation>> ListAsync(EscalationFilter filter, CancellationToken cancellationToken = default)
    {
        var query = _db.Escalations
            .Include(e => e.EscalationTier)
            .Include(e => e.EscalationStatus)
            .Include(e => e.Owner)
            .AsQueryable();

        if (!string.IsNullOrEmpty(filter.Status)) query = query.Where(e => e.EscalationStatus.Code == filter.Status);
        if (!string.IsNullOrEmpty(filter.Tier)) query = query.Where(e => e.EscalationTier.Code == filter.Tier);
        if (!string.IsNullOrEmpty(filter.EntityType)) query = query.Where(e => e.EntityType == filter.EntityType);

        return await query.OrderByDescending(e => e.OpenedAt).ToListAsync(cancellationToken);
    }

    public async Task<Escalation?> GetAsync(Guid escalationId, CancellationToken cancellationToken = default)
    {
        return await _db.Escalations
            .Include(e => e.EscalationTier)
            .Include(e => e.EscalationStatus)
            .Include(e => e.Owner)
            .SingleOrDefaultAsync(e => e.Id == escalationId, cancellationToken);
    }

    private async Task<User?> ResolveOwnerAsync(Guid tierId, CancellationToken cancellationToken)
    {
        return await _db.Users
            .Where(u => u.EscalationTierId == tierId)
            .OrderBy(u => u.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
