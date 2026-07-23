using ClosedXML.Excel;
using InnovationToImpact.Domain.Reports;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Reports;

public class EscalationsReportGenerator : IEscalationsReportGenerator
{
    private readonly InnovationDbContext _db;

    public EscalationsReportGenerator(InnovationDbContext db)
    {
        _db = db;
    }

    public async Task<byte[]> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var escalations = await _db.Escalations
            .Include(e => e.EscalationTier)
            .Include(e => e.EscalationStatus)
            .Include(e => e.Owner)
            .OrderBy(e => e.OpenedAt)
            .ToListAsync(cancellationToken);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Escalations");

        worksheet.Cell(1, 1).Value = "EntityType";
        worksheet.Cell(1, 2).Value = "EntityId";
        worksheet.Cell(1, 3).Value = "TierCode";
        worksheet.Cell(1, 4).Value = "StatusCode";
        worksheet.Cell(1, 5).Value = "OwnerNameEn";
        worksheet.Cell(1, 6).Value = "ReasonEn";
        worksheet.Cell(1, 7).Value = "OpenedAt";
        worksheet.Cell(1, 8).Value = "ResolutionEn";

        var row = 2;
        foreach (var escalation in escalations)
        {
            worksheet.Cell(row, 1).Value = escalation.EntityType;
            worksheet.Cell(row, 2).Value = escalation.EntityId.ToString();
            worksheet.Cell(row, 3).Value = escalation.EscalationTier.Code;
            worksheet.Cell(row, 4).Value = escalation.EscalationStatus.Code;
            worksheet.Cell(row, 5).Value = escalation.Owner?.FullNameEn ?? string.Empty;
            worksheet.Cell(row, 6).Value = escalation.ReasonEn;
            worksheet.Cell(row, 7).Value = escalation.OpenedAt;
            worksheet.Cell(row, 8).Value = escalation.ResolutionEn ?? string.Empty;
            row++;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
