using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class ChallengeConfigurationTests
{
    [Fact]
    public void SaveChanges_PersistsChallengeLinkedToStrategicTheme()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var themeId = db.StrategicThemes.First().Id;

        var challenge = new Challenge
        {
            Id = Guid.NewGuid(),
            StrategicThemeId = themeId,
            TextAr = "تحدٍ تجريبي",
            TextEn = "Sample challenge",
            SortOrder = 0,
            IsActive = true,
        };
        db.Challenges.Add(challenge);
        db.SaveChanges();

        using var readDb = fixture.CreateContext();
        var loaded = readDb.Challenges.Single(c => c.Id == challenge.Id);
        Assert.Equal("Sample challenge", loaded.TextEn);
        Assert.Equal(themeId, loaded.StrategicThemeId);
    }

    [Fact]
    public void SaveChanges_PersistsIdeaTeamMembersLinkedToIdea()
    {
        using var fixture = new SqliteContextFixture();
        using var seedDb = fixture.CreateContext();
        var submitterId = Guid.NewGuid();
        seedDb.Users.Add(new User { Id = submitterId, SamAccountName = "s1", Email = "s1@gac-demo.sa", FullNameAr = "s1", FullNameEn = "s1" });
        var themeId = seedDb.StrategicThemes.First().Id;
        var draftStatus = seedDb.IdeaStatuses.Single(s => s.Code == "draft");
        var ideaId = Guid.NewGuid();
        seedDb.Ideas.Add(new Idea
        {
            Id = ideaId,
            Code = "IDEA-0001",
            TitleAr = "ا", TitleEn = "T",
            ProblemStatementAr = "م", ProblemStatementEn = "P",
            ProposedSolutionAr = "ح", ProposedSolutionEn = "S",
            ExpectedBenefitsAr = "ف", ExpectedBenefitsEn = "B",
            StrategicThemeId = themeId,
            IdeaStatusId = draftStatus.Id,
            SubmitterId = submitterId,
            ParticipationType = "team",
        });
        seedDb.SaveChanges();

        using var writeDb = fixture.CreateContext();
        writeDb.IdeaTeamMembers.Add(new IdeaTeamMember { Id = Guid.NewGuid(), IdeaId = ideaId, Name = "Member One", Email = "m1@example.com", SortOrder = 0 });
        writeDb.IdeaTeamMembers.Add(new IdeaTeamMember { Id = Guid.NewGuid(), IdeaId = ideaId, Name = "Member Two", Email = "m2@example.com", SortOrder = 1 });
        writeDb.SaveChanges();

        using var readDb = fixture.CreateContext();
        var members = readDb.IdeaTeamMembers.Where(m => m.IdeaId == ideaId).OrderBy(m => m.SortOrder).ToList();
        Assert.Equal(2, members.Count);
        Assert.Equal("Member One", members[0].Name);
    }
}
