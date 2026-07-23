using InnovationToImpact.Domain.Briefing;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Invitations;
using InnovationToImpact.Domain.Roster;
using InnovationToImpact.Domain.Sla;
using InnovationToImpact.Infrastructure.Data;
using InnovationToImpact.Infrastructure.Data.Configurations;
using InnovationToImpact.Infrastructure.Roster;
using InnovationToImpact.Infrastructure.Scheduling;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class ReminderSchedulerHostedServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;

    private class FakeSlaScanOrchestrator : ISlaScanOrchestrator
    {
        public int CallCount { get; private set; }
        public Task<SlaScanOrchestratorResult> ScanAndEscalateAsync(CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new SlaScanOrchestratorResult(0, 0, 0, 0));
        }
    }

    private class FakeInvitationReminderProcessor : IInvitationReminderProcessor
    {
        public int CallCount { get; private set; }
        public Task<InvitationReminderResult> ProcessAsync(CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new InvitationReminderResult(0, 0, 0));
        }
    }

    private class FakeWeeklyBriefingProcessor : IWeeklyBriefingProcessor
    {
        public int CallCount { get; private set; }
        public Task<WeeklyBriefingResult> GenerateAsync(CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new WeeklyBriefingResult(0, 0, 0, 0, 0, 0));
        }
    }

    private class FakeRoleInvitationReminderProcessor : IRoleInvitationReminderProcessor
    {
        public int CallCount { get; private set; }
        public Task<RoleInvitationReminderResult> ProcessAsync(CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(new RoleInvitationReminderResult(0, 0, 0));
        }
    }

    private readonly FakeSlaScanOrchestrator _slaOrchestrator = new();
    private readonly FakeInvitationReminderProcessor _reminderProcessor = new();
    private readonly FakeWeeklyBriefingProcessor _briefingProcessor = new();
    private readonly FakeRoleInvitationReminderProcessor _roleInvitationReminderProcessor = new();

    public ReminderSchedulerHostedServiceTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddDbContext<InnovationDbContext>(options => options.UseSqlite(_connection));
        services.AddSingleton<ISlaScanOrchestrator>(_slaOrchestrator);
        services.AddSingleton<IInvitationReminderProcessor>(_reminderProcessor);
        services.AddSingleton<IWeeklyBriefingProcessor>(_briefingProcessor);
        services.AddSingleton<IRoleInvitationReminderProcessor>(_roleInvitationReminderProcessor);
        services.AddScoped<IRoleInvitationSettingsService, RoleInvitationSettingsService>();
        _provider = services.BuildServiceProvider();

        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
        db.Database.EnsureCreated();
    }

    [Fact]
    public async Task RunTickAsync_FirstTickOfDay_RunsSlaScanAndInvitationReminders_ButNotBriefingOnNonMonday()
    {
        var scopeFactory = _provider.GetRequiredService<IServiceScopeFactory>();
        var tuesday = new DateTime(2026, 7, 21, 6, 0, 0, DateTimeKind.Utc); // 2026-07-21 is a Tuesday

        var service = new ReminderSchedulerHostedService(scopeFactory, new NullLogger());
        await service.RunTickAsync(tuesday, scopeFactory, CancellationToken.None);

        Assert.Equal(1, _slaOrchestrator.CallCount);
        Assert.Equal(1, _reminderProcessor.CallCount);
        Assert.Equal(1, _roleInvitationReminderProcessor.CallCount);
        Assert.Equal(0, _briefingProcessor.CallCount);
    }

    [Fact]
    public async Task RunTickAsync_CalledTwiceSameDay_RunsDailyJobsOnlyOnce()
    {
        var scopeFactory = _provider.GetRequiredService<IServiceScopeFactory>();
        var morning = new DateTime(2026, 7, 21, 6, 0, 0, DateTimeKind.Utc);
        var laterSameDay = new DateTime(2026, 7, 21, 18, 0, 0, DateTimeKind.Utc);

        var service = new ReminderSchedulerHostedService(scopeFactory, new NullLogger());
        await service.RunTickAsync(morning, scopeFactory, CancellationToken.None);
        await service.RunTickAsync(laterSameDay, scopeFactory, CancellationToken.None);

        Assert.Equal(1, _slaOrchestrator.CallCount);
        Assert.Equal(1, _reminderProcessor.CallCount);
        Assert.Equal(1, _roleInvitationReminderProcessor.CallCount);
    }

    [Fact]
    public async Task RunTickAsync_Monday_AlsoRunsWeeklyBriefing()
    {
        var scopeFactory = _provider.GetRequiredService<IServiceScopeFactory>();
        var monday = new DateTime(2026, 7, 20, 6, 0, 0, DateTimeKind.Utc); // 2026-07-20 is a Monday

        var service = new ReminderSchedulerHostedService(scopeFactory, new NullLogger());
        await service.RunTickAsync(monday, scopeFactory, CancellationToken.None);

        Assert.Equal(1, _briefingProcessor.CallCount);
    }

    [Fact]
    public async Task RunTickAsync_SettingsDisabled_SkipsInvitationReminders_ButStillRunsSlaScan()
    {
        using (var scope = _provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
            var settings = await db.InvitationReminderSettings.SingleAsync(s => s.Id == InvitationReminderSettingsConfiguration.SingletonId);
            settings.Enabled = false;
            await db.SaveChangesAsync();
        }

        var scopeFactory = _provider.GetRequiredService<IServiceScopeFactory>();
        var tuesday = new DateTime(2026, 7, 21, 6, 0, 0, DateTimeKind.Utc);

        var service = new ReminderSchedulerHostedService(scopeFactory, new NullLogger());
        await service.RunTickAsync(tuesday, scopeFactory, CancellationToken.None);

        Assert.Equal(1, _slaOrchestrator.CallCount);
        Assert.Equal(0, _reminderProcessor.CallCount);
        Assert.Equal(1, _roleInvitationReminderProcessor.CallCount);
    }

    public void Dispose()
    {
        _provider.Dispose();
        _connection.Dispose();
    }
}

internal class NullLogger : Microsoft.Extensions.Logging.ILogger<ReminderSchedulerHostedService>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => false;
    public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
}
