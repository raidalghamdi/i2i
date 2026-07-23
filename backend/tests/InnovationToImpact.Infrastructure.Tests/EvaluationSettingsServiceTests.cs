using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Evaluations;
using InnovationToImpact.Infrastructure.Evaluations;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Tests;

public class EvaluationSettingsServiceTests
{
    private static async Task SetRowAsync(SqliteContextFixture fixture, string valueJson)
    {
        using var db = fixture.CreateContext();
        var row = await db.AdminSettings.SingleOrDefaultAsync(s => s.Key == "pass_threshold");
        if (row is null) { db.AdminSettings.Add(new AdminSetting { Key = "pass_threshold", ValueJson = valueJson }); }
        else { row.ValueJson = valueJson; }
        await db.SaveChangesAsync();
    }

    private static Guid SeedUser(SqliteContextFixture fixture)
    {
        using var db = fixture.CreateContext();
        var id = Guid.NewGuid();
        db.Users.Add(new User
        {
            Id = id,
            SamAccountName = "actor-" + id.ToString("N"),
            Email = id.ToString("N") + "@gac-demo.sa",
            FullNameAr = "a",
            FullNameEn = "a",
        });
        db.SaveChanges();
        return id;
    }

    private static async Task DeleteRowAsync(SqliteContextFixture fixture)
    {
        using var db = fixture.CreateContext();
        var row = await db.AdminSettings.SingleOrDefaultAsync(s => s.Key == "pass_threshold");
        if (row is not null) { db.AdminSettings.Remove(row); await db.SaveChangesAsync(); }
    }

    [Fact]
    public async Task FreshDatabase_SeedsPassThresholdAtSixDefault()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var service = new EvaluationSettingsService(db);
        // The seeded default must equal the code default (6.0), not the stale 7.
        Assert.Equal(6.0m, await service.GetPassThresholdAsync());
    }

    [Fact]
    public async Task GetPassThreshold_NoRow_ReturnsDefaultSix()
    {
        using var fixture = new SqliteContextFixture();
        await DeleteRowAsync(fixture);
        using var db = fixture.CreateContext();
        var service = new EvaluationSettingsService(db);
        Assert.Equal(6.0m, await service.GetPassThresholdAsync());
    }

    [Fact]
    public async Task GetPassThreshold_ParsesStoredDecimal()
    {
        using var fixture = new SqliteContextFixture();
        await SetRowAsync(fixture, "6.5");
        using var db = fixture.CreateContext();
        var service = new EvaluationSettingsService(db);
        Assert.Equal(6.5m, await service.GetPassThresholdAsync());
    }

    [Fact]
    public async Task GetPassThreshold_GarbageValue_FallsBackToDefault()
    {
        using var fixture = new SqliteContextFixture();
        await SetRowAsync(fixture, "not-a-number");
        using var db = fixture.CreateContext();
        var service = new EvaluationSettingsService(db);
        Assert.Equal(6.0m, await service.GetPassThresholdAsync());
    }

    [Fact]
    public async Task Update_ValidValue_PersistsAndReturnsSuccess()
    {
        using var fixture = new SqliteContextFixture();
        var actor = SeedUser(fixture);
        using (var db = fixture.CreateContext())
        {
            var service = new EvaluationSettingsService(db);
            var result = await service.UpdatePassThresholdAsync(7.5m, actor);
            Assert.True(result.Success);
            Assert.Equal(7.5m, result.Value);
        }
        using var verify = fixture.CreateContext();
        var verifyService = new EvaluationSettingsService(verify);
        var settings = await verifyService.GetAsync();
        Assert.Equal(7.5m, settings.PassThreshold);
        Assert.NotNull(settings.UpdatedAt);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(10.1)]
    public async Task Update_OutOfRange_FailsAndPersistsNothing(double invalid)
    {
        using var fixture = new SqliteContextFixture();
        await SetRowAsync(fixture, "6.0");
        using var db = fixture.CreateContext();
        var service = new EvaluationSettingsService(db);
        var result = await service.UpdatePassThresholdAsync((decimal)invalid, Guid.NewGuid());
        Assert.False(result.Success);
        Assert.Equal(6.0m, await service.GetPassThresholdAsync());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    public async Task Update_BoundaryValue_SucceedsAndPersists(int boundary)
    {
        using var fixture = new SqliteContextFixture();
        var actor = SeedUser(fixture);
        using (var db = fixture.CreateContext())
        {
            var service = new EvaluationSettingsService(db);
            var result = await service.UpdatePassThresholdAsync(boundary, actor);
            Assert.True(result.Success);
            Assert.Equal(boundary, result.Value);
        }
        using var verify = fixture.CreateContext();
        var verifyService = new EvaluationSettingsService(verify);
        Assert.Equal(boundary, await verifyService.GetPassThresholdAsync());
    }

    [Fact]
    public async Task Update_NoExistingRow_InsertsNewRow()
    {
        using var fixture = new SqliteContextFixture();
        var actor = SeedUser(fixture);
        await DeleteRowAsync(fixture);

        using (var db = fixture.CreateContext())
        {
            var service = new EvaluationSettingsService(db);
            var result = await service.UpdatePassThresholdAsync(6.5m, actor);
            Assert.True(result.Success);
        }

        using var verify = fixture.CreateContext();
        var verifyService = new EvaluationSettingsService(verify);
        Assert.Equal(6.5m, await verifyService.GetPassThresholdAsync());
    }
}
