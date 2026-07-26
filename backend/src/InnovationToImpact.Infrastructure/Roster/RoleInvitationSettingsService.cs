using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Roster;
using InnovationToImpact.Infrastructure.Data;
using InnovationToImpact.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Roster;

public class RoleInvitationSettingsService : IRoleInvitationSettingsService
{
    private readonly InnovationDbContext _db;

    public RoleInvitationSettingsService(InnovationDbContext db) { _db = db; }

    public Task<RoleInvitationSettings> GetAsync(CancellationToken cancellationToken = default) =>
        _db.RoleInvitationSettings.SingleAsync(s => s.Id == RoleInvitationSettingsConfiguration.SingletonId, cancellationToken);

    public async Task<RoleInvitationSettings> UpdateAsync(RoleInvitationSettingsInput input, Guid actorId, CancellationToken cancellationToken = default)
    {
        var settings = await GetAsync(cancellationToken);
        if (input.Enabled.HasValue) settings.Enabled = input.Enabled.Value;
        if (input.DefaultExpiresDays.HasValue) settings.DefaultExpiresDays = input.DefaultExpiresDays.Value;
        if (input.ReminderGapHours.HasValue) settings.ReminderGapHours = input.ReminderGapHours.Value;
        if (input.MaxReminders.HasValue) settings.MaxReminders = input.MaxReminders.Value;
        settings.UpdatedAt = DateTime.UtcNow;
        settings.UpdatedByUserId = actorId;
        await _db.SaveChangesAsync(cancellationToken);
        return settings;
    }
}
