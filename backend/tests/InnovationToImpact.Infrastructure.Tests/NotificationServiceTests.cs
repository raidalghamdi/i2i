using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Notifications;
using InnovationToImpact.Infrastructure.Data;
using InnovationToImpact.Infrastructure.Notifications;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

// Deliberately NOT IClassFixture<SqliteContextFixture> -- matching the established fix from
// prior Phase 2 plans (fresh fixture per test method, not class-shared).
public class NotificationServiceTests : IDisposable
{
    private readonly SqliteContextFixture _fixture = new();

    private sealed class FakeNotificationPublisher : INotificationPublisher
    {
        public Guid? PublishedUserId { get; private set; }
        public Notification? PublishedNotification { get; private set; }
        public int PublishCount { get; private set; }

        public Task PublishAsync(Guid userId, Notification notification, CancellationToken cancellationToken = default)
        {
            PublishedUserId = userId;
            PublishedNotification = notification;
            PublishCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingNotificationPublisher : INotificationPublisher
    {
        public Task PublishAsync(Guid userId, Notification notification, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("simulated publish failure");
    }

    private static Guid SeedUser(InnovationDbContext db)
    {
        var userId = Guid.NewGuid();
        db.Users.Add(new User { Id = userId, SamAccountName = "user1", Email = "user1@gac-demo.sa", FullNameAr = "user1", FullNameEn = "user1" });
        db.SaveChanges();
        return userId;
    }

    [Fact]
    public async Task CreatesNotificationRow_AndCallsPublisherWithSameData()
    {
        using var db = _fixture.CreateContext();
        var userId = SeedUser(db);
        var publisher = new FakeNotificationPublisher();
        var service = new NotificationService(db, publisher);

        var notification = await service.CreateAndPublishAsync(
            userId, "idea_status_changed", "عنوان", "Title", "نص", "Body", "/ideas/123", "{\"ideaId\":\"123\"}", CancellationToken.None);

        var stored = await db.Notifications.SingleAsync(n => n.Id == notification.Id);
        Assert.Equal(userId, stored.UserId);
        Assert.Equal("idea_status_changed", stored.NotificationType);
        Assert.Equal("Title", stored.TitleEn);
        Assert.Null(stored.ReadAt);

        Assert.Equal(1, publisher.PublishCount);
        Assert.Equal(userId, publisher.PublishedUserId);
        Assert.Equal(notification.Id, publisher.PublishedNotification!.Id);
    }

    [Fact]
    public async Task PublisherFailure_DoesNotPreventNotificationFromBeingPersisted()
    {
        using var db = _fixture.CreateContext();
        var userId = SeedUser(db);
        var service = new NotificationService(db, new ThrowingNotificationPublisher());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAndPublishAsync(userId, "type", "ar", "en", "ar", "en", null, null, CancellationToken.None));

        Assert.True(await db.Notifications.AnyAsync(n => n.UserId == userId));
    }

    public void Dispose() => _fixture.Dispose();
}
