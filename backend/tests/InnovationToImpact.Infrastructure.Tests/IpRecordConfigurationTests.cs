using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class IpRecordConfigurationTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public IpRecordConfigurationTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    private static (Guid ideaId, Guid patentTypeId) SeedPrerequisites(InnovationDbContext context, string suffix)
    {
        var submitterId = Guid.NewGuid();
        var themeId = Guid.NewGuid();

        context.Users.Add(new User { Id = submitterId, SamAccountName = $"submitter-{suffix}", Email = $"sub-{suffix}@gac-demo.sa", FullNameAr = "أ", FullNameEn = "Submitter" });
        context.StrategicThemes.Add(new StrategicTheme { Id = themeId, NameAr = "محور", NameEn = "Theme", OwnerId = submitterId });
        context.SaveChanges();

        var ideaStatusId = context.IdeaStatuses.Single(s => s.Code == "draft").Id;
        var ideaId = Guid.NewGuid();
        context.Ideas.Add(new Idea
        {
            Id = ideaId,
            Code = $"IDEA-{suffix}",
            TitleAr = "فكرة", TitleEn = "Idea",
            ProblemStatementAr = "أ", ProblemStatementEn = "A",
            ProposedSolutionAr = "أ", ProposedSolutionEn = "A",
            ExpectedBenefitsAr = "أ", ExpectedBenefitsEn = "A",
            StrategicThemeId = themeId,
            IdeaStatusId = ideaStatusId,
            CurrentStage = 8,
            SubmitterId = submitterId,
        });
        context.SaveChanges();

        var patentTypeId = context.IpTypes.Single(t => t.Code == "patent").Id;
        return (ideaId, patentTypeId);
    }

    [Fact]
    public void SavesIpRecordLinkedToAnIdea()
    {
        Guid recordId;
        Guid ideaId;

        using (var context = _fixture.CreateContext())
        {
            var (seededIdeaId, patentTypeId) = SeedPrerequisites(context, "ip-t2a");
            ideaId = seededIdeaId;

            var record = new IpRecord
            {
                Id = Guid.NewGuid(),
                IdeaId = ideaId,
                IpTypeId = patentTypeId,
                OwnershipPartyAr = "الجهة",
                OwnershipPartyEn = "Party",
                ConfidentialityTermsAr = "شروط",
                ConfidentialityTermsEn = "Terms",
                ParticipationConditionsAr = "شروط المشاركة",
                ParticipationConditionsEn = "Conditions",
                NdaRequired = true,
            };
            recordId = record.Id;

            context.IpRecords.Add(record);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var record = context.IpRecords
                .Include(r => r.IpType)
                .Single(r => r.Id == recordId);
            Assert.Equal("patent", record.IpType.Code);
            Assert.Equal(ideaId, record.IdeaId);
            Assert.True(record.NdaRequired);
        }
    }

    [Fact]
    public void AllowsNullIdeaIdForAStandaloneIpRecord()
    {
        using var context = _fixture.CreateContext();
        var (_, patentTypeId) = SeedPrerequisites(context, "ip-t2b");

        context.IpRecords.Add(new IpRecord
        {
            Id = Guid.NewGuid(),
            IdeaId = null,
            IpTypeId = patentTypeId,
            OwnershipPartyAr = "أ", OwnershipPartyEn = "A",
            ConfidentialityTermsAr = "أ", ConfidentialityTermsEn = "A",
            ParticipationConditionsAr = "أ", ParticipationConditionsEn = "A",
            NdaRequired = false,
        });

        var exception = Record.Exception(() => context.SaveChanges());
        Assert.Null(exception);
    }
}
