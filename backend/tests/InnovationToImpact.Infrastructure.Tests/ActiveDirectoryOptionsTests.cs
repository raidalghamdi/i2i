using InnovationToImpact.Infrastructure.Auth;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class ActiveDirectoryOptionsTests
{
    [Fact]
    public void DefaultsCacheTtlToSixtyMinutes()
    {
        var options = new ActiveDirectoryOptions();

        Assert.Equal(60, options.CacheTtlMinutes);
    }
}
