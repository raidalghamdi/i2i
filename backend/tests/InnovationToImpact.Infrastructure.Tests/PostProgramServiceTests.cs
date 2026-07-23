using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Domain.Ideas;
using InnovationToImpact.Domain.PostProgram;
using InnovationToImpact.Infrastructure.PostProgram;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Tests;

public class PostProgramServiceTests
{
    private static Guid SeedUser(SqliteContextFixture fixture)
    {
        using var db = fixture.CreateContext();
        var id = Guid.NewGuid();
        db.Users.Add(new User { Id = id, SamAccountName = "u" + id.ToString("N"), Email = id.ToString("N") + "@gac-demo.sa", FullNameAr = "u", FullNameEn = "u" });
        db.SaveChanges();
        return id;
    }

    private static async Task<Guid> SeedIdeaAsync(SqliteContextFixture fixture, string statusCode)
    {
        var submitterId = SeedUser(fixture);
        using var db = fixture.CreateContext();
        var status = db.IdeaStatuses.Single(s => s.Code == statusCode);
        var theme = db.StrategicThemes.First();
        var activity = new Activity { Id = Guid.NewGuid(), NameAr = "n", NameEn = "Activity", Type = "event", Status = "open", CreatedById = submitterId };
        db.Activities.Add(activity);
        var id = Guid.NewGuid();
        db.Ideas.Add(new Idea
        {
            Id = id, Code = "IDEA-" + id.ToString("N")[..8], TitleAr = "ف", TitleEn = "Idea",
            ProblemStatementAr = "م", ProblemStatementEn = "P", ProposedSolutionAr = "ح", ProposedSolutionEn = "S",
            ExpectedBenefitsAr = "ب", ExpectedBenefitsEn = "B",
            StrategicThemeId = theme.Id, ActivityId = activity.Id, SubmitterId = submitterId,
            IdeaStatusId = status.Id, ParticipationType = "individual",
        });
        await db.SaveChangesAsync();
        return id;
    }

    [Theory]
    [InlineData(IdeaStatusCodes.Approved, IdeaStatusCodes.InPilot, 6)]
    [InlineData(IdeaStatusCodes.InPilot, IdeaStatusCodes.InMeasurement, 7)]
    [InlineData(IdeaStatusCodes.InMeasurement, IdeaStatusCodes.InScaling, 8)]
    public async Task Advance_ValidNextStep_UpdatesStatusAndStage(string from, string to, int expectedStage)
    {
        using var fixture = new SqliteContextFixture();
        var ideaId = await SeedIdeaAsync(fixture, from);
        using var db = fixture.CreateContext();
        var service = new PostProgramService(db);

        var result = await service.AdvanceAsync(ideaId, to, default);

        Assert.Equal(PostProgramAdvanceStatus.Success, result.Status);
        Assert.Equal(to, result.Idea!.IdeaStatus.Code);
        using var verify = fixture.CreateContext();
        var reloaded = verify.Ideas.Include(i => i.IdeaStatus).Single(i => i.Id == ideaId);
        Assert.Equal(to, reloaded.IdeaStatus.Code);
        Assert.Equal(expectedStage, reloaded.CurrentStage);
    }

    [Fact]
    public async Task Advance_SkippingAStage_IsInvalidTransition()
    {
        using var fixture = new SqliteContextFixture();
        var ideaId = await SeedIdeaAsync(fixture, IdeaStatusCodes.Approved);
        using var db = fixture.CreateContext();
        var result = await new PostProgramService(db).AdvanceAsync(ideaId, IdeaStatusCodes.InScaling, default);
        Assert.Equal(PostProgramAdvanceStatus.InvalidTransition, result.Status);
    }

    [Fact]
    public async Task Advance_Backwards_IsInvalidTransition()
    {
        using var fixture = new SqliteContextFixture();
        var ideaId = await SeedIdeaAsync(fixture, IdeaStatusCodes.InMeasurement);
        using var db = fixture.CreateContext();
        var result = await new PostProgramService(db).AdvanceAsync(ideaId, IdeaStatusCodes.InPilot, default);
        Assert.Equal(PostProgramAdvanceStatus.InvalidTransition, result.Status);
    }

    [Fact]
    public async Task Advance_NonPostProgramStage_IsInvalidStage()
    {
        using var fixture = new SqliteContextFixture();
        var ideaId = await SeedIdeaAsync(fixture, IdeaStatusCodes.Approved);
        using var db = fixture.CreateContext();
        var result = await new PostProgramService(db).AdvanceAsync(ideaId, IdeaStatusCodes.Committee, default);
        Assert.Equal(PostProgramAdvanceStatus.InvalidStage, result.Status);
    }

    [Fact]
    public async Task Advance_MissingIdea_IsNotFound()
    {
        using var fixture = new SqliteContextFixture();
        using var db = fixture.CreateContext();
        var result = await new PostProgramService(db).AdvanceAsync(Guid.NewGuid(), IdeaStatusCodes.InPilot, default);
        Assert.Equal(PostProgramAdvanceStatus.NotFound, result.Status);
    }

    [Theory]
    [InlineData(IdeaStatusCodes.InPilot)]
    [InlineData(IdeaStatusCodes.InMeasurement)]
    [InlineData(IdeaStatusCodes.InScaling)]
    public async Task Advance_FromTerminalInScaling_AnyTarget_IsInvalidTransition(string target)
    {
        using var fixture = new SqliteContextFixture();
        var ideaId = await SeedIdeaAsync(fixture, IdeaStatusCodes.InScaling);
        using var db = fixture.CreateContext();

        var result = await new PostProgramService(db).AdvanceAsync(ideaId, target, default);

        Assert.Equal(PostProgramAdvanceStatus.InvalidTransition, result.Status);
    }

    [Fact]
    public async Task GetPostProgramIdeas_ReturnsOnlyPostProgramStatuses()
    {
        using var fixture = new SqliteContextFixture();
        await SeedIdeaAsync(fixture, IdeaStatusCodes.Approved);
        await SeedIdeaAsync(fixture, IdeaStatusCodes.InPilot);
        await SeedIdeaAsync(fixture, IdeaStatusCodes.Committee); // excluded
        using var db = fixture.CreateContext();
        var ideas = await new PostProgramService(db).GetPostProgramIdeasAsync(default);
        Assert.All(ideas, i => Assert.Contains(i.IdeaStatus.Code, new[]
        {
            IdeaStatusCodes.Approved, IdeaStatusCodes.InPilot, IdeaStatusCodes.InMeasurement, IdeaStatusCodes.InScaling,
        }));
        Assert.Equal(2, ideas.Count);
    }

    [Fact]
    public async Task GetPostProgramIdeas_OrdersByCodeAscending()
    {
        using var fixture = new SqliteContextFixture();
        // Seeded first (so insertion order would put it first) but given the larger code,
        // to prove the result is ordered by Code and not by insertion order.
        var insertedFirstId = await SeedIdeaAsync(fixture, IdeaStatusCodes.InPilot);
        var insertedSecondId = await SeedIdeaAsync(fixture, IdeaStatusCodes.Approved);

        using (var setup = fixture.CreateContext())
        {
            var insertedFirst = await setup.Ideas.SingleAsync(i => i.Id == insertedFirstId);
            insertedFirst.Code = "IDEA-0002";
            var insertedSecond = await setup.Ideas.SingleAsync(i => i.Id == insertedSecondId);
            insertedSecond.Code = "IDEA-0001";
            await setup.SaveChangesAsync();
        }

        using var db = fixture.CreateContext();
        var ideas = await new PostProgramService(db).GetPostProgramIdeasAsync(default);

        Assert.Equal(2, ideas.Count);
        Assert.Equal(new[] { "IDEA-0001", "IDEA-0002" }, ideas.Select(i => i.Code));
        Assert.Equal(insertedSecondId, ideas[0].Id);
        Assert.Equal(insertedFirstId, ideas[1].Id);
    }
}
