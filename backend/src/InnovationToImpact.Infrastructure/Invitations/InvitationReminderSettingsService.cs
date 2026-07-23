using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Invitations;
using InnovationToImpact.Infrastructure.Data;
using InnovationToImpact.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Invitations;

public class InvitationReminderSettingsService : IInvitationReminderSettingsService
{
    private readonly InnovationDbContext _db;

    public InvitationReminderSettingsService(InnovationDbContext db)
    {
        _db = db;
    }

    public Task<InvitationReminderSettings> GetAsync(CancellationToken cancellationToken = default) =>
        _db.InvitationReminderSettings.SingleAsync(s => s.Id == InvitationReminderSettingsConfiguration.SingletonId, cancellationToken);

    public async Task<InvitationReminderSettings> UpdateAsync(InvitationReminderSettingsInput input, Guid actorId, CancellationToken cancellationToken = default)
    {
        var settings = await GetAsync(cancellationToken);

        if (input.Enabled.HasValue) settings.Enabled = input.Enabled.Value;
        if (input.CronExpression is not null) settings.CronExpression = input.CronExpression;
        if (input.Timezone is not null) settings.Timezone = input.Timezone;
        if (input.StopAfterNReminders.HasValue) settings.StopAfterNReminders = input.StopAfterNReminders.Value;
        if (input.GapHours.HasValue) settings.GapHours = input.GapHours.Value;
        if (input.ExpiresDays.HasValue) settings.ExpiresDays = input.ExpiresDays.Value;
        if (input.FromName is not null) settings.FromName = input.FromName;
        if (input.FromEmail is not null) settings.FromEmail = input.FromEmail;
        if (input.ProgramNameAr is not null) settings.ProgramNameAr = input.ProgramNameAr;
        if (input.ProgramNameEn is not null) settings.ProgramNameEn = input.ProgramNameEn;
        settings.UpdatedAt = DateTime.UtcNow;
        settings.UpdatedByUserId = actorId;

        await _db.SaveChangesAsync(cancellationToken);
        return settings;
    }
}
