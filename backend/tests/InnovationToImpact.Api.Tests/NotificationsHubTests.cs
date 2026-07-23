using InnovationToImpact.Domain.Auth;
using InnovationToImpact.Domain.Notifications;
using InnovationToImpact.Infrastructure.Auth;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace InnovationToImpact.Api.Tests;

public class NotificationsHubTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;

    public NotificationsHubTests(WebApplicationFactory<Program> factory)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var lookup = new FakeAdIdentityLookupService(new[]
        {
            new AdIdentity("notifyuser1", "Notify User", "notifyuser1@gac-demo.sa", null, null, null),
        });

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<InnovationDbContext>>();
                services.AddDbContext<InnovationDbContext>(options => options.UseSqlite(_connection));

                services.RemoveAll<IAdIdentityLookupService>();
                services.AddSingleton<IAdIdentityLookupService>(lookup);

                using var scope = services.BuildServiceProvider().CreateScope();
                scope.ServiceProvider.GetRequiredService<InnovationDbContext>().Database.EnsureCreated();
            });
        });
    }

    [Fact]
    public async Task ConnectedClient_ReceivesPublishedNotification()
    {
        var connection = new HubConnectionBuilder()
            .WithUrl($"{_factory.Server.BaseAddress}hubs/notifications", options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
                options.Headers.Add("X-Dev-User", "notifyuser1");
            })
            .Build();

        var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<object>("ReceiveNotification", payload => received.TrySetResult(payload?.ToString() ?? string.Empty));

        await connection.StartAsync();
        try
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<InnovationDbContext>();
            var userId = await db.Users.Where(u => u.SamAccountName == "notifyuser1").Select(u => u.Id).SingleAsync();

            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            await notificationService.CreateAndPublishAsync(
                userId, "test_notification", "عنوان", "Test Title", "نص", "Test Body", null, null, CancellationToken.None);

            var completed = await Task.WhenAny(received.Task, Task.Delay(TimeSpan.FromSeconds(10)));
            Assert.Same(received.Task, completed);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    public void Dispose() => _connection.Dispose();
}
