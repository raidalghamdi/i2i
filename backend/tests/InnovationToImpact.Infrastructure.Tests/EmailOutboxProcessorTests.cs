using InnovationToImpact.Domain.Email;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Data;
using InnovationToImpact.Infrastructure.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

// Deliberately NOT IClassFixture<SqliteContextFixture> -- these tests assert absolute
// EmailOutboxProcessingResult counts (e.g. "Processed == 2"), which only holds against a table
// no other test method has already written pending rows into, and xUnit does not guarantee
// [Fact] execution order. A fresh fixture per test method isolates each test's tables, the same
// fix already applied in the Audit Hash-Chain Reimplementation plan.
public class EmailOutboxProcessorTests : IDisposable
{
    private readonly SqliteContextFixture _fixture = new();

    private sealed class StubEmailSender : IEmailSender
    {
        private readonly bool _succeeds;

        public StubEmailSender(bool succeeds)
        {
            _succeeds = succeeds;
        }

        public Task<EmailSendResult> SendAsync(string toEmail, string subject, string bodyHtml, string? bodyText, CancellationToken cancellationToken = default) =>
            Task.FromResult(_succeeds
                ? new EmailSendResult(true, "stub", $"stub-{Guid.NewGuid()}", null)
                : new EmailSendResult(false, "stub", null, "simulated failure"));
    }

    private sealed class ThrowingEmailSender : IEmailSender
    {
        public Task<EmailSendResult> SendAsync(string toEmail, string subject, string bodyHtml, string? bodyText, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("simulated transient provider failure");
    }

    private static EmailOutboxOptions NewOptions(int maxAttempts = 5) => new() { MaxAttempts = maxAttempts };

    private static async Task<Guid> SeedPendingEmailAsync(InnovationDbContext db, int attempts = 0)
    {
        var pendingStatus = await db.EmailOutboxStatuses.SingleAsync(s => s.Code == EmailOutboxStatusCodes.Pending);
        var id = Guid.NewGuid();
        db.EmailOutboxes.Add(new EmailOutbox
        {
            Id = id,
            ToEmail = "recipient@gac-demo.sa",
            Subject = "Test Subject",
            BodyHtml = "<p>Body</p>",
            Category = "test",
            EmailOutboxStatusId = pendingStatus.Id,
            Attempts = attempts,
        });
        await db.SaveChangesAsync();
        return id;
    }

    [Fact]
    public async Task SuccessfulSend_TransitionsToSent_WritesEmailLog_IncrementsAttempts()
    {
        using var db = _fixture.CreateContext();
        var id = await SeedPendingEmailAsync(db);

        var processor = new EmailOutboxProcessor(db, new StubEmailSender(succeeds: true), Options.Create(NewOptions()));
        var result = await processor.ProcessPendingAsync(CancellationToken.None);

        Assert.Equal(new EmailOutboxProcessingResult(1, 1, 0), result);

        var updated = await db.EmailOutboxes.Include(o => o.EmailOutboxStatus).SingleAsync(o => o.Id == id);
        Assert.Equal(EmailOutboxStatusCodes.Sent, updated.EmailOutboxStatus.Code);
        Assert.Equal(1, updated.Attempts);

        var log = await db.EmailLogs.Include(l => l.EmailLogStatus).SingleAsync(l => l.ToEmail == "recipient@gac-demo.sa");
        Assert.Equal(EmailLogStatusCodes.Sent, log.EmailLogStatus.Code);
        Assert.Equal("stub", log.Provider);
    }

    [Fact]
    public async Task FailedSendBelowMaxAttempts_RevertsToPending_NoEmailLogYet()
    {
        using var db = _fixture.CreateContext();
        var id = await SeedPendingEmailAsync(db, attempts: 1);

        var processor = new EmailOutboxProcessor(db, new StubEmailSender(succeeds: false), Options.Create(NewOptions(maxAttempts: 5)));
        var result = await processor.ProcessPendingAsync(CancellationToken.None);

        Assert.Equal(new EmailOutboxProcessingResult(1, 0, 0), result);

        var updated = await db.EmailOutboxes.Include(o => o.EmailOutboxStatus).SingleAsync(o => o.Id == id);
        Assert.Equal(EmailOutboxStatusCodes.Pending, updated.EmailOutboxStatus.Code);
        Assert.Equal(2, updated.Attempts);

        Assert.False(await db.EmailLogs.AnyAsync(l => l.ToEmail == "recipient@gac-demo.sa"));
    }

    [Fact]
    public async Task FailedSendAtMaxAttempts_TransitionsToFailed_WritesEmailLog()
    {
        using var db = _fixture.CreateContext();
        var id = await SeedPendingEmailAsync(db, attempts: 4);

        var processor = new EmailOutboxProcessor(db, new StubEmailSender(succeeds: false), Options.Create(NewOptions(maxAttempts: 5)));
        var result = await processor.ProcessPendingAsync(CancellationToken.None);

        Assert.Equal(new EmailOutboxProcessingResult(1, 0, 1), result);

        var updated = await db.EmailOutboxes.Include(o => o.EmailOutboxStatus).SingleAsync(o => o.Id == id);
        Assert.Equal(EmailOutboxStatusCodes.Failed, updated.EmailOutboxStatus.Code);
        Assert.Equal(5, updated.Attempts);

        var log = await db.EmailLogs.Include(l => l.EmailLogStatus).SingleAsync(l => l.ToEmail == "recipient@gac-demo.sa");
        Assert.Equal(EmailLogStatusCodes.Failed, log.EmailLogStatus.Code);
    }

    [Fact]
    public async Task OnlyProcessesRowsWithPendingStatus()
    {
        using var db = _fixture.CreateContext();
        var sentStatus = await db.EmailOutboxStatuses.SingleAsync(s => s.Code == EmailOutboxStatusCodes.Sent);
        db.EmailOutboxes.Add(new EmailOutbox
        {
            Id = Guid.NewGuid(),
            ToEmail = "already-sent@gac-demo.sa",
            Subject = "Already Sent",
            BodyHtml = "<p>Body</p>",
            Category = "test",
            EmailOutboxStatusId = sentStatus.Id,
            Attempts = 1,
        });
        await db.SaveChangesAsync();
        await SeedPendingEmailAsync(db);

        var processor = new EmailOutboxProcessor(db, new StubEmailSender(succeeds: true), Options.Create(NewOptions()));
        var result = await processor.ProcessPendingAsync(CancellationToken.None);

        Assert.Equal(1, result.Processed);
    }

    [Fact]
    public async Task SendAsyncThrows_TreatedAsFailedAttempt_RevertsToPending()
    {
        // Regression test found during the final whole-branch review: a real IEmailSender
        // (SMTP/SendGrid/etc.) throws on transient failures rather than returning
        // Success:false -- the processor must treat a thrown exception the same as a returned
        // failure so Attempts/status still advance, instead of aborting the whole batch.
        using var db = _fixture.CreateContext();
        var id = await SeedPendingEmailAsync(db, attempts: 1);

        var processor = new EmailOutboxProcessor(db, new ThrowingEmailSender(), Options.Create(NewOptions(maxAttempts: 5)));
        var result = await processor.ProcessPendingAsync(CancellationToken.None);

        Assert.Equal(new EmailOutboxProcessingResult(1, 0, 0), result);

        var updated = await db.EmailOutboxes.Include(o => o.EmailOutboxStatus).SingleAsync(o => o.Id == id);
        Assert.Equal(EmailOutboxStatusCodes.Pending, updated.EmailOutboxStatus.Code);
        Assert.Equal(2, updated.Attempts);
    }

    public void Dispose() => _fixture.Dispose();
}
