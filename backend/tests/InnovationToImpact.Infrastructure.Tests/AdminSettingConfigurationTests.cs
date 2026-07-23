using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class AdminSettingConfigurationTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public AdminSettingConfigurationTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void SeedsTopNAndPassThresholdSettings()
    {
        using var context = _fixture.CreateContext();

        var topN = context.AdminSettings.Single(s => s.Key == "top_n");
        Assert.Equal("5", topN.ValueJson);

        var passThreshold = context.AdminSettings.Single(s => s.Key == "pass_threshold");
        Assert.Equal("6.0", passThreshold.ValueJson);
    }

    [Fact]
    public void RejectsDuplicateKey()
    {
        using var context = _fixture.CreateContext();
        context.AdminSettings.Add(new AdminSetting { Key = "top_n", ValueJson = "10" });

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }
}
