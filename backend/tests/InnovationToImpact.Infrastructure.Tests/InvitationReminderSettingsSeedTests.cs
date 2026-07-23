using InnovationToImpact.Infrastructure.Data;
using InnovationToImpact.Infrastructure.Data.Configurations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class InvitationReminderSettingsSeedTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly InnovationDbContext _db;

    public InvitationReminderSettingsSeedTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<InnovationDbContext>().UseSqlite(_connection).Options;
        _db = new InnovationDbContext(options);
        _db.Database.EnsureCreated();
    }

    [Fact]
    public async Task SeededRow_HasLegacyMatchingDefaults()
    {
        var settings = await _db.InvitationReminderSettings.SingleAsync(s => s.Id == InvitationReminderSettingsConfiguration.SingletonId);

        Assert.True(settings.Enabled);
        Assert.Equal("0 9 * * 1", settings.CronExpression);
        Assert.Equal("Asia/Riyadh", settings.Timezone);
        Assert.Equal(3, settings.StopAfterNReminders);
        Assert.Equal(48, settings.GapHours);
        Assert.Equal(14, settings.ExpiresDays);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
