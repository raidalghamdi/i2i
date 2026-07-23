using ClosedXML.Excel;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Reports;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class EvaluationsReportGeneratorTests : IDisposable
{
    private readonly SqliteContextFixture _fixture = new();

    [Fact]
    public async Task GeneratesWorkbookWithOneRowPerEvaluation()
    {
        using var db = _fixture.CreateContext();
        var submitterId = Guid.NewGuid();
        db.Users.Add(new User { Id = submitterId, SamAccountName = "submitter2", Email = "submitter2@gac-demo.sa", FullNameAr = "submitter2", FullNameEn = "submitter2" });
        var evaluatorId = Guid.NewGuid();
        db.Users.Add(new User { Id = evaluatorId, SamAccountName = "evaluator5", Email = "evaluator5@gac-demo.sa", FullNameAr = "evaluator5", FullNameEn = "evaluator5" });
        db.SaveChanges();
        var themeId = db.StrategicThemes.First().Id;
        var statusId = db.IdeaStatuses.Single(s => s.Code == "evaluation").Id;

        var ideaId = Guid.NewGuid();
        db.Ideas.Add(new Idea
        {
            Id = ideaId, Code = "IDEA-EVAL-1", TitleAr = "ا", TitleEn = "Evaluated Idea",
            ProblemStatementAr = "م", ProblemStatementEn = "P", ProposedSolutionAr = "ح", ProposedSolutionEn = "S",
            ExpectedBenefitsAr = "ف", ExpectedBenefitsEn = "B", StrategicThemeId = themeId,
            IdeaStatusId = statusId, SubmitterId = submitterId,
        });
        db.SaveChanges();

        db.Evaluations.Add(new Evaluation
        {
            Id = Guid.NewGuid(), IdeaId = ideaId, EvaluatorId = evaluatorId,
            TotalScore = 7.5m, Recommendation = "pass", SubmittedAt = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc),
        });
        db.SaveChanges();

        var generator = new EvaluationsReportGenerator(db);
        var bytes = await generator.GenerateAsync(CancellationToken.None);

        Assert.True(bytes.Length > 0);

        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheet("Evaluations");

        Assert.Equal("IdeaCode", worksheet.Cell(1, 1).GetString());
        Assert.Equal("EvaluatorNameEn", worksheet.Cell(1, 2).GetString());
        Assert.Equal("TotalScore", worksheet.Cell(1, 3).GetString());

        Assert.Equal("IDEA-EVAL-1", worksheet.Cell(2, 1).GetString());
        Assert.Equal("evaluator5", worksheet.Cell(2, 2).GetString());
        Assert.Equal("7.5", worksheet.Cell(2, 3).GetString());
        Assert.Equal("pass", worksheet.Cell(2, 4).GetString());

        Assert.Equal(2, worksheet.LastRowUsed()!.RowNumber());
    }

    public void Dispose() => _fixture.Dispose();
}
