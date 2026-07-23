using InnovationToImpact.Domain.Reports.Bundle;
using Xunit;

namespace InnovationToImpact.Api.Tests;

public class ReportCatalogTests
{
    [Fact]
    public void All_twelve_report_types_have_catalog_metadata()
    {
        Assert.Equal(12, ReportTypeCodes.All.Length);
        foreach (var type in ReportTypeCodes.All)
        {
            var meta = ReportCatalog.Meta(type);
            Assert.False(string.IsNullOrWhiteSpace(meta.NameEn));
            Assert.False(string.IsNullOrWhiteSpace(meta.NameAr));
        }
    }

    [Fact]
    public void Meta_returns_null_for_unknown_type()
    {
        Assert.Null(ReportCatalog.Meta("nonsense"));
    }
}
