using ClosedXML.Excel;
using InnovationToImpact.Domain.Reports;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Reports;

public class EvaluationsReportGenerator : IEvaluationsReportGenerator
{
    private readonly InnovationDbContext _db;

    public EvaluationsReportGenerator(InnovationDbContext db)
    {
        _db = db;
    }

    public async Task<byte[]> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var evaluations = await _db.Evaluations
            .Include(e => e.Idea)
            .Include(e => e.Evaluator)
            .OrderBy(e => e.SubmittedAt)
            .ToListAsync(cancellationToken);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Evaluations");

        worksheet.Cell(1, 1).Value = "IdeaCode";
        worksheet.Cell(1, 2).Value = "EvaluatorNameEn";
        worksheet.Cell(1, 3).Value = "TotalScore";
        worksheet.Cell(1, 4).Value = "Recommendation";
        worksheet.Cell(1, 5).Value = "SubmittedAt";

        var row = 2;
        foreach (var evaluation in evaluations)
        {
            worksheet.Cell(row, 1).Value = evaluation.Idea.Code;
            worksheet.Cell(row, 2).Value = evaluation.Evaluator.FullNameEn;
            worksheet.Cell(row, 3).Value = evaluation.TotalScore.ToString();
            worksheet.Cell(row, 4).Value = evaluation.Recommendation ?? string.Empty;
            worksheet.Cell(row, 5).Value = evaluation.SubmittedAt?.ToString() ?? string.Empty;
            row++;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
