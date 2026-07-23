using InnovationToImpact.Domain.Entities;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class StrategicThemeAndActivityTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public StrategicThemeAndActivityTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void SavesStrategicThemeOwnedByAUser()
    {
        var ownerId = Guid.NewGuid();

        using (var context = _fixture.CreateContext())
        {
            context.Users.Add(new User { Id = ownerId, SamAccountName = "theme-owner-t5", Email = "owner@gac-demo.sa", FullNameAr = "أ", FullNameEn = "Owner" });
            context.StrategicThemes.Add(new StrategicTheme
            {
                Id = Guid.NewGuid(),
                NameAr = "التحول الرقمي",
                NameEn = "Digital Transformation",
                Priority = 1,
                OwnerId = ownerId
            });
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var theme = context.StrategicThemes.Single(t => t.NameEn == "Digital Transformation" && t.OwnerId == ownerId);
            Assert.Equal(ownerId, theme.OwnerId);
        }
    }

    [Fact]
    public void SavesActivityCreatedByAUser()
    {
        var creatorId = Guid.NewGuid();

        using (var context = _fixture.CreateContext())
        {
            context.Users.Add(new User { Id = creatorId, SamAccountName = "activity-creator-t5", Email = "creator@gac-demo.sa", FullNameAr = "أ", FullNameEn = "Creator" });
            context.Activities.Add(new Activity
            {
                Id = Guid.NewGuid(),
                NameAr = "هاكاثون الابتكار",
                NameEn = "Innovation Hackathon",
                Type = "hackathon",
                Status = "active",
                StartDate = new DateTime(2026, 8, 1),
                EndDate = new DateTime(2026, 8, 3),
                CreatedById = creatorId
            });
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var activity = context.Activities.Single(a => a.NameEn == "Innovation Hackathon");
            Assert.Equal(creatorId, activity.CreatedById);
        }
    }
}
