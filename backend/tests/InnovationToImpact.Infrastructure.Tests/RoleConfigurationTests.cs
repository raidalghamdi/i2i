using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class RoleConfigurationTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public RoleConfigurationTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void SavesAndReadsRoleByCode()
    {
        using (var context = _fixture.CreateContext())
        {
            context.Roles.Add(new Role
            {
                Id = Guid.NewGuid(),
                Code = "test_custom_role",
                NameAr = "دور مخصص",
                NameEn = "Custom Role",
                IsSystem = false,
                IsActive = true,
                SortOrder = 100
            });
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var role = context.Roles.Single(r => r.Code == "test_custom_role");
            Assert.Equal("Custom Role", role.NameEn);
            Assert.False(role.IsSystem);
        }
    }

    [Fact]
    public void RejectsDuplicateRoleCode()
    {
        using var context = _fixture.CreateContext();
        context.Roles.Add(new Role { Id = Guid.NewGuid(), Code = "dup", NameAr = "أ", NameEn = "A", SortOrder = 1 });
        context.SaveChanges();

        context.Roles.Add(new Role { Id = Guid.NewGuid(), Code = "dup", NameAr = "ب", NameEn = "B", SortOrder = 2 });

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }
}
