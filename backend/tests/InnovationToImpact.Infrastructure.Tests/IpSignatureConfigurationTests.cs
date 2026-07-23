using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class IpSignatureConfigurationTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public IpSignatureConfigurationTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    private static (Guid ideaId, Guid signerId) SeedPrerequisites(InnovationDbContext context, string suffix)
    {
        var submitterId = Guid.NewGuid();
        var signerId = Guid.NewGuid();
        var themeId = Guid.NewGuid();

        context.Users.Add(new User { Id = submitterId, SamAccountName = $"submitter-{suffix}", Email = $"sub-{suffix}@gac-demo.sa", FullNameAr = "أ", FullNameEn = "Submitter" });
        context.Users.Add(new User { Id = signerId, SamAccountName = $"signer-{suffix}", Email = $"signer-{suffix}@gac-demo.sa", FullNameAr = "ب", FullNameEn = "Signer" });
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

        return (ideaId, signerId);
    }

    [Fact]
    public void SavesIpSignatureWithRequiredRelationships()
    {
        Guid signatureId;

        using (var context = _fixture.CreateContext())
        {
            var (ideaId, signerId) = SeedPrerequisites(context, "sig-t3a");

            var signature = new IpSignature
            {
                Id = Guid.NewGuid(),
                IdeaId = ideaId,
                UserId = signerId,
            };
            signatureId = signature.Id;

            context.IpSignatures.Add(signature);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var signature = context.IpSignatures.Single(s => s.Id == signatureId);
            Assert.Equal("v1", signature.IpTermsVersion);
        }
    }

    [Fact]
    public void RejectsDuplicateSignatureForSameIdeaAndUser()
    {
        using var context = _fixture.CreateContext();
        var (ideaId, signerId) = SeedPrerequisites(context, "sig-t3b");

        context.IpSignatures.Add(new IpSignature { Id = Guid.NewGuid(), IdeaId = ideaId, UserId = signerId });
        context.SaveChanges();

        context.IpSignatures.Add(new IpSignature { Id = Guid.NewGuid(), IdeaId = ideaId, UserId = signerId });

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }
}
