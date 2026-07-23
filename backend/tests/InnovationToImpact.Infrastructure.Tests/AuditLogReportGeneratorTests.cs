using ClosedXML.Excel;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Reports;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class AuditLogReportGeneratorTests : IDisposable
{
    private readonly SqliteContextFixture _fixture = new();

    [Fact]
    public async Task GeneratesWorkbookWithOneRowPerAuditLogEntry()
    {
        using var db = _fixture.CreateContext();
        db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), ChainSeq = 1, RowHash = new string('a', 64), EntityType = "idea", EntityId = Guid.NewGuid(), Action = "create", OccurredAt = DateTime.UtcNow });
        db.AuditLogs.Add(new AuditLog { Id = Guid.NewGuid(), ChainSeq = 2, RowHash = new string('b', 64), PrevHash = new string('a', 64), EntityType = "idea", EntityId = Guid.NewGuid(), Action = "update", OccurredAt = DateTime.UtcNow });
        db.SaveChanges();

        var generator = new AuditLogReportGenerator(db);
        var bytes = await generator.GenerateAsync(CancellationToken.None);

        Assert.True(bytes.Length > 0);

        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheet("Audit Log");

        Assert.Equal("ChainSeq", worksheet.Cell(1, 1).GetString());
        Assert.Equal("EntityType", worksheet.Cell(1, 2).GetString());
        Assert.Equal("Action", worksheet.Cell(1, 4).GetString());

        Assert.Equal("1", worksheet.Cell(2, 1).GetString());
        Assert.Equal("create", worksheet.Cell(2, 4).GetString());
        Assert.Equal("2", worksheet.Cell(3, 1).GetString());
        Assert.Equal("update", worksheet.Cell(3, 4).GetString());

        Assert.Equal(3, worksheet.LastRowUsed()!.RowNumber());
    }

    public void Dispose() => _fixture.Dispose();
}
