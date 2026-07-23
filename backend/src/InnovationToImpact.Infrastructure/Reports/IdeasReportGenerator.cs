using ClosedXML.Excel;
using InnovationToImpact.Domain.Reports;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Reports;

public class IdeasReportGenerator : IIdeasReportGenerator
{
    private readonly InnovationDbContext _db;

    public IdeasReportGenerator(InnovationDbContext db)
    {
        _db = db;
    }

    public async Task<byte[]> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var ideas = await _db.Ideas
            .Include(i => i.IdeaStatus)
            .Include(i => i.StrategicTheme)
            .Include(i => i.Submitter)
            .OrderBy(i => i.Code)
            .ToListAsync(cancellationToken);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Ideas");

        worksheet.Cell(1, 1).Value = "Code";
        worksheet.Cell(1, 2).Value = "TitleEn";
        worksheet.Cell(1, 3).Value = "StatusCode";
        worksheet.Cell(1, 4).Value = "ThemeNameEn";
        worksheet.Cell(1, 5).Value = "SubmitterNameEn";
        worksheet.Cell(1, 6).Value = "CommitteeFinalScore";
        worksheet.Cell(1, 7).Value = "FinalRank";
        worksheet.Cell(1, 8).Value = "CreatedAt";
        worksheet.Cell(1, 9).Value = "ApprovedAt";

        var row = 2;
        foreach (var idea in ideas)
        {
            worksheet.Cell(row, 1).Value = idea.Code;
            worksheet.Cell(row, 2).Value = idea.TitleEn;
            worksheet.Cell(row, 3).Value = idea.IdeaStatus.Code;
            worksheet.Cell(row, 4).Value = idea.StrategicTheme.NameEn;
            worksheet.Cell(row, 5).Value = idea.Submitter.FullNameEn;
            worksheet.Cell(row, 6).Value = idea.CommitteeFinalScore?.ToString() ?? string.Empty;
            worksheet.Cell(row, 7).Value = idea.FinalRank?.ToString() ?? string.Empty;
            worksheet.Cell(row, 8).Value = idea.CreatedAt;
            worksheet.Cell(row, 9).Value = idea.ApprovedAt?.ToString() ?? string.Empty;
            row++;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
