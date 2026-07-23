using ClosedXML.Excel;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Reports;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class IdeasReportGeneratorTests : IDisposable
{
    private readonly SqliteContextFixture _fixture = new();

    [Fact]
    public async Task GeneratesWorkbookWithOneRowPerIdea()
    {
        using var db = _fixture.CreateContext();
        var submitterId = Guid.NewGuid();
        db.Users.Add(new User { Id = submitterId, SamAccountName = "submitter1", Email = "submitter1@gac-demo.sa", FullNameAr = "submitter1", FullNameEn = "submitter1" });
        db.SaveChanges();
        var themeId = db.StrategicThemes.First().Id;
        var draftStatusId = db.IdeaStatuses.Single(s => s.Code == "draft").Id;
        var approvedStatusId = db.IdeaStatuses.Single(s => s.Code == "approved").Id;

        db.Ideas.Add(new Idea
        {
            Id = Guid.NewGuid(), Code = "IDEA-0001", TitleAr = "ا", TitleEn = "First Idea",
            ProblemStatementAr = "م", ProblemStatementEn = "P", ProposedSolutionAr = "ح", ProposedSolutionEn = "S",
            ExpectedBenefitsAr = "ف", ExpectedBenefitsEn = "B", StrategicThemeId = themeId,
            IdeaStatusId = draftStatusId, SubmitterId = submitterId,
        });
        db.Ideas.Add(new Idea
        {
            Id = Guid.NewGuid(), Code = "IDEA-0002", TitleAr = "ا", TitleEn = "Second Idea",
            ProblemStatementAr = "م", ProblemStatementEn = "P", ProposedSolutionAr = "ح", ProposedSolutionEn = "S",
            ExpectedBenefitsAr = "ف", ExpectedBenefitsEn = "B", StrategicThemeId = themeId,
            IdeaStatusId = approvedStatusId, SubmitterId = submitterId, CommitteeFinalScore = 8.5m, FinalRank = 1,
        });
        db.SaveChanges();

        var generator = new IdeasReportGenerator(db);
        var bytes = await generator.GenerateAsync(CancellationToken.None);

        Assert.True(bytes.Length > 0);

        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheet("Ideas");

        Assert.Equal("Code", worksheet.Cell(1, 1).GetString());
        Assert.Equal("TitleEn", worksheet.Cell(1, 2).GetString());
        Assert.Equal("StatusCode", worksheet.Cell(1, 3).GetString());

        Assert.Equal("IDEA-0001", worksheet.Cell(2, 1).GetString());
        Assert.Equal("draft", worksheet.Cell(2, 3).GetString());
        Assert.Equal("IDEA-0002", worksheet.Cell(3, 1).GetString());
        Assert.Equal("approved", worksheet.Cell(3, 3).GetString());
        Assert.Equal("8.5", worksheet.Cell(3, 6).GetString());
        Assert.Equal("1", worksheet.Cell(3, 7).GetString());

        Assert.Equal(3, worksheet.LastRowUsed()!.RowNumber());
    }

    public void Dispose() => _fixture.Dispose();
}
