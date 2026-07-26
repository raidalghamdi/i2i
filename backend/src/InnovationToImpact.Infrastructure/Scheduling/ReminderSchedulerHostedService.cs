using InnovationToImpact.Domain.Briefing;
using InnovationToImpact.Domain.Invitations;
using InnovationToImpact.Domain.Roster;
using InnovationToImpact.Domain.Sla;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InnovationToImpact.Infrastructure.Scheduling;

public class ReminderSchedulerHostedService : BackgroundService
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromMinutes(15);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReminderSchedulerHostedService> _logger;

    private DateTime? _lastDailyRunDateUtc;
    private DateTime? _lastWeeklyRunDateUtc;

    public ReminderSchedulerHostedService(IServiceScopeFactory scopeFactory, ILogger<ReminderSchedulerHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Deliberately wait for the first interval before the first tick (not a
        // do-while) so a host restart doesn't force an eager scan/reminder pass,
        // and so short-lived WebApplicationFactory test hosts never overlap a
        // tick — the daily/weekly gates in RunTickAsync only care about calendar
        // boundaries, so this delay has no effect on correctness.
        using var timer = new PeriodicTimer(TickInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await RunTickAsync(DateTime.UtcNow, _scopeFactory, stoppingToken);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "ReminderSchedulerHostedService tick failed");
            }
        }
    }

    internal async Task RunTickAsync(DateTime nowUtc, IServiceScopeFactory scopeFactory, CancellationToken cancellationToken)
    {
        var today = nowUtc.Date;

        if (_lastDailyRunDateUtc != today)
        {
            using (var scope = scopeFactory.CreateScope())
            {
                var orchestrator = scope.ServiceProvider.GetRequiredService<ISlaScanOrchestrator>();
                await orchestrator.ScanAndEscalateAsync(cancellationToken);
            }

            using (var scope = scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
                var settings = await db.InvitationReminderSettings.SingleAsync(cancellationToken);
                if (settings.Enabled)
                {
                    var processor = scope.ServiceProvider.GetRequiredService<IInvitationReminderProcessor>();
                    await processor.ProcessAsync(cancellationToken);
                }
            }

            using (var scope = scopeFactory.CreateScope())
            {
                // Always invoke the processor (not gated on settings.Enabled): it must
                // unconditionally expire past-deadline invitations and cancel their
                // PendingRoleGrants regardless of whether reminder emails are enabled.
                // The processor itself decides internally whether to also queue reminders.
                var processor = scope.ServiceProvider.GetRequiredService<IRoleInvitationReminderProcessor>();
                await processor.ProcessAsync(cancellationToken);
            }

            _lastDailyRunDateUtc = today;
        }

        if (today.DayOfWeek == DayOfWeek.Monday && _lastWeeklyRunDateUtc != today)
        {
            using var scope = scopeFactory.CreateScope();
            var briefingProcessor = scope.ServiceProvider.GetRequiredService<IWeeklyBriefingProcessor>();
            await briefingProcessor.GenerateAsync(cancellationToken);
            _lastWeeklyRunDateUtc = today;
        }
    }
}
