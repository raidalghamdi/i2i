using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class EmailTemplateConfigurationTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public EmailTemplateConfigurationTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    private Guid EnsureAdminRole()
    {
        using var context = _fixture.CreateContext();
        var existingAdmin = context.Roles.FirstOrDefault(r => r.Code == "admin");
        if (existingAdmin != null)
            return existingAdmin.Id;

        var adminRole = new Role
        {
            Id = Guid.NewGuid(),
            Code = "admin",
            NameAr = "مسؤول",
            NameEn = "Admin",
            IsSystem = true,
            IsActive = true,
            SortOrder = 1
        };
        context.Roles.Add(adminRole);
        context.SaveChanges();
        return adminRole.Id;
    }

    [Fact]
    public void SavesRoleScopedEmailTemplate()
    {
        EnsureAdminRole();
        Guid templateId;

        using (var context = _fixture.CreateContext())
        {
            var adminRoleId = context.Roles.Single(r => r.Code == "admin").Id;

            var template = new EmailTemplate
            {
                Id = Guid.NewGuid(),
                Kind = "admin_digest",
                RoleId = adminRoleId,
                SubjectAr = "الموضوع",
                SubjectEn = "Subject",
                BodyAr = "النص",
                BodyEn = "Body",
            };
            templateId = template.Id;

            context.EmailTemplates.Add(template);
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var template = context.EmailTemplates
                .Include(t => t.Role)
                .Single(t => t.Id == templateId);
            Assert.Equal("admin", template.Role!.Code);
            Assert.True(template.IsActive);
            Assert.False(template.IsBroadcast);
        }
    }

    [Fact]
    public void AllowsNullRoleForARoleAgnosticBroadcastTemplate()
    {
        using var context = _fixture.CreateContext();

        context.EmailTemplates.Add(new EmailTemplate
        {
            Id = Guid.NewGuid(),
            Kind = "welcome",
            RoleId = null,
            SubjectAr = "أ", SubjectEn = "A",
            BodyAr = "أ", BodyEn = "A",
            IsBroadcast = true,
        });

        var exception = Record.Exception(() => context.SaveChanges());
        Assert.Null(exception);
    }

    [Fact]
    public void RejectsDuplicateKind()
    {
        using var context = _fixture.CreateContext();

        context.EmailTemplates.Add(new EmailTemplate
        {
            Id = Guid.NewGuid(), Kind = "dup-kind", RoleId = null,
            SubjectAr = "أ", SubjectEn = "A", BodyAr = "أ", BodyEn = "A",
        });
        context.SaveChanges();

        context.EmailTemplates.Add(new EmailTemplate
        {
            Id = Guid.NewGuid(), Kind = "dup-kind", RoleId = null,
            SubjectAr = "ب", SubjectEn = "B", BodyAr = "ب", BodyEn = "B",
        });

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }
}
