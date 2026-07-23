using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class NotificationConfigurationTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public NotificationConfigurationTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void SavesUnreadNotificationForAUser()
    {
        Guid notificationId;
        Guid userId;

        using (var context = _fixture.CreateContext())
        {
            userId = Guid.NewGuid();
            context.Users.Add(new User { Id = userId, SamAccountName = "notif-t2a", Email = "notif-t2a@gac-demo.sa", FullNameAr = "أ", FullNameEn = "User" });
            context.SaveChanges();

            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                NotificationType = "idea_submitted",
                TitleAr = "عنوان",
                TitleEn = "Title",
                BodyAr = "نص",
                BodyEn = "Body",
            };
            notificationId = notification.Id;

            context.Notifications.Add(notification);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var notification = context.Notifications.Single(n => n.Id == notificationId);
            Assert.Equal(userId, notification.UserId);
            Assert.Null(notification.ReadAt);
            Assert.Equal("idea_submitted", notification.NotificationType);
        }
    }

    [Fact]
    public void AllowsMarkingANotificationAsRead()
    {
        using var context = _fixture.CreateContext();
        var userId = Guid.NewGuid();
        context.Users.Add(new User { Id = userId, SamAccountName = "notif-t2b", Email = "notif-t2b@gac-demo.sa", FullNameAr = "أ", FullNameEn = "User" });
        context.SaveChanges();

        context.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            NotificationType = "evaluation_assigned",
            TitleAr = "أ", TitleEn = "A",
            BodyAr = "أ", BodyEn = "A",
            ReadAt = DateTime.UtcNow,
        });

        var exception = Record.Exception(() => context.SaveChanges());
        Assert.Null(exception);
    }
}
