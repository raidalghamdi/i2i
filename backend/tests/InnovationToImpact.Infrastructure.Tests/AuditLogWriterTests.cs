using InnovationToImpact.Domain.Audit;
using InnovationToImpact.Infrastructure.Audit;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class AuditLogWriterTests : IDisposable
{
    private readonly SqliteContextFixture _fixture = new();

    [Fact]
    public async Task FirstAppend_HasNullPrevHashAndChainSeqOne()
    {
        using var context = _fixture.CreateContext();
        var writer = new AuditLogWriter(context);

        var entry = await writer.AppendAsync("idea", Guid.NewGuid(), "create", null, "{}", CancellationToken.None);

        Assert.Equal(1, entry.ChainSeq);
        Assert.Null(entry.PrevHash);
        Assert.Equal(64, entry.RowHash.Length);

        var reloaded = await context.AuditLogs.SingleAsync(a => a.Id == entry.Id);
        Assert.Equal(entry.RowHash, reloaded.RowHash);
    }

    [Fact]
    public async Task SecondAppend_ChainsFromFirstsRowHash()
    {
        using var context = _fixture.CreateContext();
        var writer = new AuditLogWriter(context);

        var first = await writer.AppendAsync("idea", Guid.NewGuid(), "create", null, "{}", CancellationToken.None);
        var second = await writer.AppendAsync("idea", first.EntityId, "update", null, "{\"status\":\"submitted\"}", CancellationToken.None);

        Assert.Equal(2, second.ChainSeq);
        Assert.Equal(first.RowHash, second.PrevHash);
    }

    [Fact]
    public async Task ConcurrentAppends_ProduceGapFreeUniqueSequenceWithNoExceptions()
    {
        using var context = _fixture.CreateContext();
        var writer = new AuditLogWriter(context);

        var tasks = Enumerable.Range(0, 10)
            .Select(i => writer.AppendAsync("idea", Guid.NewGuid(), "create", null, $"{{\"i\":{i}}}", CancellationToken.None));
        var results = await Task.WhenAll(tasks);

        var sequences = results.Select(r => r.ChainSeq).OrderBy(s => s).ToList();
        Assert.Equal(Enumerable.Range(1, 10).Select(i => (long)i), sequences);
        Assert.Equal(10, sequences.Distinct().Count());
    }

    public void Dispose() => _fixture.Dispose();
}
