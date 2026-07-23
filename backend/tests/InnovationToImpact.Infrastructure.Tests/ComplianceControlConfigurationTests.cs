using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class ComplianceControlConfigurationTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public ComplianceControlConfigurationTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    private static (Guid standardBodyId, Guid statusId) SeedLookups(InnovationDbContext context)
    {
        var standardBodyId = context.StandardBodies.Single(b => b.Code == "nca").Id;
        var statusId = context.ComplianceControlStatuses.Single(s => s.Code == "in_progress").Id;
        return (standardBodyId, statusId);
    }

    [Fact]
    public void SavesComplianceControlWithAssignedOwner()
    {
        Guid controlId;
        Guid ownerId;

        using (var context = _fixture.CreateContext())
        {
            var (standardBodyId, statusId) = SeedLookups(context);

            ownerId = Guid.NewGuid();
            context.Users.Add(new User { Id = ownerId, SamAccountName = "owner-cc-t2a", Email = "owner-cc-t2a@gac-demo.sa", FullNameAr = "أ", FullNameEn = "Owner" });
            context.SaveChanges();

            var control = new ComplianceControl
            {
                Id = Guid.NewGuid(),
                ControlCode = "NCA-ECC-1.1",
                StandardBodyId = standardBodyId,
                TitleAr = "عنوان الضابط",
                TitleEn = "Control Title",
                DescriptionAr = "وصف الضابط",
                DescriptionEn = "Control Description",
                OwnerId = ownerId,
                ComplianceControlStatusId = statusId,
            };
            controlId = control.Id;

            context.ComplianceControls.Add(control);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var control = context.ComplianceControls
                .Include(c => c.StandardBody)
                .Include(c => c.ComplianceControlStatus)
                .Single(c => c.Id == controlId);
            Assert.Equal("nca", control.StandardBody.Code);
            Assert.Equal("in_progress", control.ComplianceControlStatus.Code);
            Assert.Equal(ownerId, control.OwnerId);
        }
    }

    [Fact]
    public void AllowsNullOwnerForANewlySeededControl()
    {
        using var context = _fixture.CreateContext();
        var (standardBodyId, statusId) = SeedLookups(context);

        context.ComplianceControls.Add(new ComplianceControl
        {
            Id = Guid.NewGuid(),
            ControlCode = "NCA-ECC-1.2",
            StandardBodyId = standardBodyId,
            TitleAr = "أ", TitleEn = "A",
            DescriptionAr = "أ", DescriptionEn = "A",
            OwnerId = null,
            ComplianceControlStatusId = statusId,
        });

        var exception = Record.Exception(() => context.SaveChanges());
        Assert.Null(exception);
    }

    [Fact]
    public void RejectsDuplicateControlCode()
    {
        using var context = _fixture.CreateContext();
        var (standardBodyId, statusId) = SeedLookups(context);

        context.ComplianceControls.Add(new ComplianceControl
        {
            Id = Guid.NewGuid(),
            ControlCode = "NCA-ECC-DUP",
            StandardBodyId = standardBodyId,
            TitleAr = "أ", TitleEn = "A",
            DescriptionAr = "أ", DescriptionEn = "A",
            ComplianceControlStatusId = statusId,
        });
        context.SaveChanges();

        context.ComplianceControls.Add(new ComplianceControl
        {
            Id = Guid.NewGuid(),
            ControlCode = "NCA-ECC-DUP",
            StandardBodyId = standardBodyId,
            TitleAr = "ب", TitleEn = "B",
            DescriptionAr = "ب", DescriptionEn = "B",
            ComplianceControlStatusId = statusId,
        });

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }
}
