using InnovationToImpact.Domain.Roster;
using InnovationToImpact.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class RoleInvitationConfigurationTests : IDisposable
{
    private readonly SqliteContextFixture _fixture = new();

    [Fact]
    public void SeedsExactlyFourStatusesWithCorrectCodes()
    {
        using var db = _fixture.CreateContext();
        var codes = db.RoleInvitationStatuses.OrderBy(s => s.SortOrder).Select(s => s.Code).ToList();
        Assert.Equal(new[] { RoleInvitationStatusCodes.Pending, RoleInvitationStatusCodes.Applied, RoleInvitationStatusCodes.Expired, RoleInvitationStatusCodes.Withdrawn }, codes);
    }

    [Fact]
    public void SeedsExactlyOneSettingsRowWithLegacyMatchingDefaults()
    {
        using var db = _fixture.CreateContext();
        var settings = db.RoleInvitationSettings.Single();
        Assert.Equal(RoleInvitationSettingsConfiguration.SingletonId, settings.Id);
        Assert.True(settings.Enabled);
        Assert.Equal(14, settings.DefaultExpiresDays);
        Assert.Equal(48, settings.ReminderGapHours);
        Assert.Equal(3, settings.MaxReminders);
    }

    public void Dispose() => _fixture.Dispose();
}
