using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class UserBadgeConfigurationTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public UserBadgeConfigurationTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    private static (Guid userId, Guid badgeId) SeedPrerequisites(InnovationDbContext context, string suffix)
    {
        var userId = Guid.NewGuid();
        context.Users.Add(new User { Id = userId, SamAccountName = $"badge-user-{suffix}", Email = $"badge-user-{suffix}@gac-demo.sa", FullNameAr = "أ", FullNameEn = "User" });

        var badgeId = Guid.NewGuid();
        context.Badges.Add(new Badge { Id = badgeId, Code = $"badge-{suffix}", NameAr = "أ", NameEn = "A" });

        context.SaveChanges();
        return (userId, badgeId);
    }

    [Fact]
    public void SavesAwardAndCascadesWhenUserIsDeleted()
    {
        Guid userId;
        Guid userBadgeId;

        using (var context = _fixture.CreateContext())
        {
            var (seededUserId, badgeId) = SeedPrerequisites(context, "t2a");
            userId = seededUserId;

            var userBadge = new UserBadge
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                BadgeId = badgeId,
            };
            userBadgeId = userBadge.Id;

            context.UserBadges.Add(userBadge);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            Assert.True(context.UserBadges.Any(ub => ub.Id == userBadgeId));

            context.Users.Remove(context.Users.Single(u => u.Id == userId));
            context.SaveChanges();

            Assert.False(context.UserBadges.Any(ub => ub.Id == userBadgeId));
        }
    }

    [Fact]
    public void RejectsDuplicateBadgeAwardForSameUser()
    {
        using var context = _fixture.CreateContext();
        var (userId, badgeId) = SeedPrerequisites(context, "t2b");

        context.UserBadges.Add(new UserBadge { Id = Guid.NewGuid(), UserId = userId, BadgeId = badgeId });
        context.SaveChanges();

        context.UserBadges.Add(new UserBadge { Id = Guid.NewGuid(), UserId = userId, BadgeId = badgeId });

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }
}
