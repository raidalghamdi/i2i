using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Phases;
using InnovationToImpact.Infrastructure.Phases;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class PhaseScheduleServiceTests
{
    [Fact]
    public async Task ListAsync_ReturnsAllSevenSeededPhasesInOrder()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var service = new PhaseScheduleService(db);

        var result = await service.ListAsync();

        Assert.Equal(7, result.Count);
        Assert.Equal(0, result[0].Idx);
        Assert.Equal("submission", result[0].Code);
        Assert.Equal(6, result[6].Idx);
        Assert.Equal("benefits_tracking", result[6].Code);
    }

    [Fact]
    public async Task UpdateAsync_ValidIdx_SetsDatesAndAuditFields()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var service = new PhaseScheduleService(db);
        var actorId = Guid.NewGuid();
        db.Users.Add(new User { Id = actorId, SamAccountName = "admin1", Email = "admin1@gac-demo.sa", FullNameAr = "م1", FullNameEn = "Admin1" });
        await db.SaveChangesAsync();
        var startsAt = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var endsAt = new DateTime(2026, 8, 31, 0, 0, 0, DateTimeKind.Utc);

        var result = await service.UpdateAsync(0, startsAt, endsAt, actorId);

        Assert.Equal(PhaseScheduleCommandStatus.Success, result.Status);
        Assert.Equal(startsAt, result.Entity!.StartsAt);
        Assert.Equal(endsAt, result.Entity.EndsAt);
        Assert.Equal(actorId, result.Entity.UpdatedBy);
    }

    [Fact]
    public async Task UpdateAsync_IdxOutOfRange_ReturnsNotFound()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var service = new PhaseScheduleService(db);

        var result = await service.UpdateAsync(7, null, null, Guid.NewGuid());

        Assert.Equal(PhaseScheduleCommandStatus.NotFound, result.Status);
    }
}
