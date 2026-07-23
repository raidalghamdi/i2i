using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class ReportGenerationConfigurationTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public ReportGenerationConfigurationTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    private static (Guid reportTitleId, Guid requesterId) SeedPrerequisites(InnovationDbContext context, string suffix)
    {
        var reportTitleId = Guid.NewGuid();
        context.ReportTitles.Add(new ReportTitle { Id = reportTitleId, Key = $"report-{suffix}", TitleAr = "أ", TitleEn = "A", SortOrder = 1 });

        var requesterId = Guid.NewGuid();
        context.Users.Add(new User { Id = requesterId, SamAccountName = $"requester-{suffix}", Email = $"requester-{suffix}@gac-demo.sa", FullNameAr = "أ", FullNameEn = "Requester" });

        context.SaveChanges();
        return (reportTitleId, requesterId);
    }

    [Fact]
    public void SavesInProgressGenerationWithNullCompletedAt()
    {
        Guid generationId;

        using (var context = _fixture.CreateContext())
        {
            var (reportTitleId, requesterId) = SeedPrerequisites(context, "t3a");

            var generation = new ReportGeneration
            {
                Id = Guid.NewGuid(),
                ReportTitleId = reportTitleId,
                Format = "pdf",
                Status = "pending",
                RequestedById = requesterId,
            };
            generationId = generation.Id;

            context.ReportGenerations.Add(generation);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var generation = context.ReportGenerations.Include(g => g.ReportTitle).Single(g => g.Id == generationId);
            Assert.Equal("pdf", generation.Format);
            Assert.Null(generation.CompletedAt);
            Assert.Null(generation.FileUrl);
        }
    }

    [Fact]
    public void RejectsReportTitleIdThatDoesNotExist()
    {
        using var context = _fixture.CreateContext();
        var (_, requesterId) = SeedPrerequisites(context, "t3b");

        context.ReportGenerations.Add(new ReportGeneration
        {
            Id = Guid.NewGuid(),
            ReportTitleId = Guid.NewGuid(),
            Format = "pdf",
            Status = "pending",
            RequestedById = requesterId,
        });

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }
}
