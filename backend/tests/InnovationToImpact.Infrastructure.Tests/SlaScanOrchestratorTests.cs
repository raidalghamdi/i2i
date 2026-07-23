using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Data;
using InnovationToImpact.Infrastructure.Escalations;
using InnovationToImpact.Infrastructure.Sla;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class SlaScanOrchestratorTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly InnovationDbContext _db;

    public SlaScanOrchestratorTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<InnovationDbContext>().UseSqlite(_connection).Options;
        _db = new InnovationDbContext(options);
        _db.Database.EnsureCreated();
    }

    [Fact]
    public async Task ScanAndEscalateAsync_NoBreaches_OpensNoEscalations()
    {
        var scanner = new SlaScanner(_db);
        var escalationService = new EscalationService(_db);
        var orchestrator = new SlaScanOrchestrator(scanner, escalationService, _db);

        var result = await orchestrator.ScanAndEscalateAsync();

        Assert.Equal(0, result.EscalationsOpened);
        Assert.Equal(0, _db.Escalations.Count());
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
