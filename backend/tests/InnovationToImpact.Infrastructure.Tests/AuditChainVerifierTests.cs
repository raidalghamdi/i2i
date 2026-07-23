using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Audit;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

// Deliberately NOT IClassFixture<SqliteContextFixture> -- same reasoning as
// AuditLogWriterTests.cs (Task 1): these tests assert absolute chain state
// (e.g. "the chain is empty"), which only holds against a table no other test
// method in this class has already written rows into, and xUnit does not
// guarantee [Fact] execution order. A fresh fixture per test method isolates
// each test's AuditLogs table, matching the fix applied in Task 1.
public class AuditChainVerifierTests : IDisposable
{
    private readonly SqliteContextFixture _fixture = new();

    [Fact]
    public async Task EmptyChain_IsValid()
    {
        using var context = _fixture.CreateContext();
        var verifier = new AuditChainVerifier(context);

        var result = await verifier.VerifyAsync(CancellationToken.None);

        Assert.True(result.IsValid);
        Assert.Null(result.BrokenAtChainSeq);
    }

    [Fact]
    public async Task UnbrokenChainWrittenByAuditLogWriter_IsValid()
    {
        // Verification happens through a genuinely fresh context (not the one the writer
        // used) -- this is deliberate. EF Core's change tracker returns the same in-memory
        // tracked entity (with its original DateTimeKind, etc.) for anything still tracked,
        // which silently hides any read-path serialization drift between what was written
        // and what a real read would produce. A test that never reloads from the database
        // cannot catch that class of bug (see the OccurredAt/Ticks correction above).
        using (var writeContext = _fixture.CreateContext())
        {
            var writer = new AuditLogWriter(writeContext);
            await writer.AppendAsync("idea", Guid.NewGuid(), "create", null, "{}", CancellationToken.None);
            await writer.AppendAsync("idea", Guid.NewGuid(), "create", null, "{}", CancellationToken.None);
            await writer.AppendAsync("idea", Guid.NewGuid(), "create", null, "{}", CancellationToken.None);
        }

        using var freshContext = _fixture.CreateContext();
        var verifier = new AuditChainVerifier(freshContext);
        var result = await verifier.VerifyAsync(CancellationToken.None);

        Assert.True(result.IsValid);
        Assert.Null(result.BrokenAtChainSeq);
    }

    [Fact]
    public async Task TamperedPayloadOnMiddleRow_IsDetectedAtCorrectChainSeq()
    {
        AuditLog tampered;
        using (var writeContext = _fixture.CreateContext())
        {
            var writer = new AuditLogWriter(writeContext);
            await writer.AppendAsync("idea", Guid.NewGuid(), "create", null, "{}", CancellationToken.None);
            tampered = await writer.AppendAsync("idea", Guid.NewGuid(), "create", null, "{\"original\":true}", CancellationToken.None);
            await writer.AppendAsync("idea", Guid.NewGuid(), "create", null, "{}", CancellationToken.None);
        }

        using (var tamperContext = _fixture.CreateContext())
        {
            var toTamper = await tamperContext.AuditLogs.SingleAsync(a => a.ChainSeq == tampered.ChainSeq);
            toTamper.Payload = "{\"tampered\":true}";
            await tamperContext.SaveChangesAsync();
        }

        using var freshContext = _fixture.CreateContext();
        var verifier = new AuditChainVerifier(freshContext);
        var result = await verifier.VerifyAsync(CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Equal(tampered.ChainSeq, result.BrokenAtChainSeq);
    }

    [Fact]
    public async Task TamperedPrevHashField_IsDetected()
    {
        AuditLog second;
        using (var writeContext = _fixture.CreateContext())
        {
            var writer = new AuditLogWriter(writeContext);
            await writer.AppendAsync("idea", Guid.NewGuid(), "create", null, "{}", CancellationToken.None);
            second = await writer.AppendAsync("idea", Guid.NewGuid(), "create", null, "{}", CancellationToken.None);
        }

        using (var tamperContext = _fixture.CreateContext())
        {
            var toTamper = await tamperContext.AuditLogs.SingleAsync(a => a.ChainSeq == second.ChainSeq);
            toTamper.PrevHash = new string('f', 64);
            await tamperContext.SaveChangesAsync();
        }

        using var freshContext = _fixture.CreateContext();
        var verifier = new AuditChainVerifier(freshContext);
        var result = await verifier.VerifyAsync(CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Equal(second.ChainSeq, result.BrokenAtChainSeq);
    }

    public void Dispose() => _fixture.Dispose();
}
