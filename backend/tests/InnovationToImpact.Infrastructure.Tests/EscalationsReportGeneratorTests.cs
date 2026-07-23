using ClosedXML.Excel;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Reports;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class EscalationsReportGeneratorTests : IDisposable
{
    private readonly SqliteContextFixture _fixture = new();

    [Fact]
    public async Task GeneratesWorkbookWithOneRowPerEscalation()
    {
        using var db = _fixture.CreateContext();
        var ownerId = Guid.NewGuid();
        db.Users.Add(new User { Id = ownerId, SamAccountName = "owner1", Email = "owner1@gac-demo.sa", FullNameAr = "owner1", FullNameEn = "owner1" });
        db.SaveChanges();
        var tierId = db.EscalationTiers.Single(t => t.Code == "manager").Id;
        var statusId = db.EscalationStatuses.Single(s => s.Code == "open").Id;

        db.Escalations.Add(new Escalation
        {
            Id = Guid.NewGuid(), EntityType = "evaluation", EntityId = Guid.NewGuid(),
            EscalationTierId = tierId, EscalationStatusId = statusId, OwnerId = ownerId,
            ReasonAr = "أ", ReasonEn = "SLA breach", OpenedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
        });
        db.SaveChanges();

        var generator = new EscalationsReportGenerator(db);
        var bytes = await generator.GenerateAsync(CancellationToken.None);

        Assert.True(bytes.Length > 0);

        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheet("Escalations");

        Assert.Equal("EntityType", worksheet.Cell(1, 1).GetString());
        Assert.Equal("TierCode", worksheet.Cell(1, 3).GetString());
        Assert.Equal("StatusCode", worksheet.Cell(1, 4).GetString());

        Assert.Equal("evaluation", worksheet.Cell(2, 1).GetString());
        Assert.Equal("manager", worksheet.Cell(2, 3).GetString());
        Assert.Equal("open", worksheet.Cell(2, 4).GetString());
        Assert.Equal("owner1", worksheet.Cell(2, 5).GetString());
        Assert.Equal("SLA breach", worksheet.Cell(2, 6).GetString());

        Assert.Equal(2, worksheet.LastRowUsed()!.RowNumber());
    }

    public void Dispose() => _fixture.Dispose();
}
