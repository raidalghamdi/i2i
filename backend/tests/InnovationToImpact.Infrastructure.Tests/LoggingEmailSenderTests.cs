using InnovationToImpact.Infrastructure.Email;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class LoggingEmailSenderTests
{
    [Fact]
    public async Task AlwaysSucceedsWithLoggingProviderAndSyntheticMessageId()
    {
        var sender = new LoggingEmailSender(NullLogger<LoggingEmailSender>.Instance);

        var result = await sender.SendAsync("user@gac-demo.sa", "Subject", "<p>Body</p>", "Body", CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("logging", result.Provider);
        Assert.NotNull(result.ProviderMessageId);
        Assert.StartsWith("logging-", result.ProviderMessageId);
        Assert.Null(result.ErrorMessage);
    }
}
