using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class ApprovalChainConfigurationTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public ApprovalChainConfigurationTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void SavesActiveApprovalChainForAnEntityType()
    {
        Guid chainId;

        using (var context = _fixture.CreateContext())
        {
            var chain = new ApprovalChain
            {
                Id = Guid.NewGuid(),
                Code = "idea-standard-t2a",
                NameAr = "سلسلة الموافقة القياسية",
                NameEn = "Standard Approval Chain",
                EntityType = "idea",
            };
            chainId = chain.Id;

            context.ApprovalChains.Add(chain);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var chain = context.ApprovalChains.Single(c => c.Id == chainId);
            Assert.Equal("idea", chain.EntityType);
            Assert.True(chain.IsActive);
        }
    }

    [Fact]
    public void RejectsDuplicateApprovalChainCode()
    {
        using var context = _fixture.CreateContext();

        context.ApprovalChains.Add(new ApprovalChain { Id = Guid.NewGuid(), Code = "dup-code-t2b", NameAr = "أ", NameEn = "A", EntityType = "idea" });
        context.SaveChanges();

        context.ApprovalChains.Add(new ApprovalChain { Id = Guid.NewGuid(), Code = "dup-code-t2b", NameAr = "ب", NameEn = "B", EntityType = "pilot" });

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }
}
