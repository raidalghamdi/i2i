using ClosedXML.Excel;
using InnovationToImpact.Domain.Reports;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Reports;

public class AuditLogReportGenerator : IAuditLogReportGenerator
{
    private readonly InnovationDbContext _db;

    public AuditLogReportGenerator(InnovationDbContext db)
    {
        _db = db;
    }

    public async Task<byte[]> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var entries = await _db.AuditLogs
            .OrderBy(a => a.ChainSeq)
            .ToListAsync(cancellationToken);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Audit Log");

        worksheet.Cell(1, 1).Value = "ChainSeq";
        worksheet.Cell(1, 2).Value = "EntityType";
        worksheet.Cell(1, 3).Value = "EntityId";
        worksheet.Cell(1, 4).Value = "Action";
        worksheet.Cell(1, 5).Value = "ActorId";
        worksheet.Cell(1, 6).Value = "OccurredAt";
        worksheet.Cell(1, 7).Value = "RowHash";

        var row = 2;
        foreach (var entry in entries)
        {
            worksheet.Cell(row, 1).Value = entry.ChainSeq;
            worksheet.Cell(row, 2).Value = entry.EntityType;
            worksheet.Cell(row, 3).Value = entry.EntityId.ToString();
            worksheet.Cell(row, 4).Value = entry.Action;
            worksheet.Cell(row, 5).Value = entry.ActorId?.ToString() ?? string.Empty;
            worksheet.Cell(row, 6).Value = entry.OccurredAt;
            worksheet.Cell(row, 7).Value = entry.RowHash;
            row++;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
