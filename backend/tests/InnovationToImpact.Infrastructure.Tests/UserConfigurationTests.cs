using InnovationToImpact.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class UserConfigurationTests : IClassFixture<SqliteContextFixture>
{
    private readonly SqliteContextFixture _fixture;

    public UserConfigurationTests(SqliteContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void AssignsMultipleRolesToOneUser()
    {
        var userId = Guid.NewGuid();
        var adminRoleId = Guid.NewGuid();
        var evaluatorRoleId = Guid.NewGuid();

        using (var context = _fixture.CreateContext())
        {
            context.Roles.Add(new Role { Id = adminRoleId, Code = "admin-t3", NameAr = "مسؤول", NameEn = "Admin", SortOrder = 1 });
            context.Roles.Add(new Role { Id = evaluatorRoleId, Code = "evaluator-t3", NameAr = "مقيّم", NameEn = "Evaluator", SortOrder = 2 });
            context.Users.Add(new User
            {
                Id = userId,
                SamAccountName = "jsmith",
                Email = "jsmith@gac-demo.sa",
                FullNameAr = "جون سميث",
                FullNameEn = "John Smith",
                Department = "Innovation",
                Title = "Analyst"
            });
            context.SaveChanges();

            context.Set<UserRole>().Add(new UserRole { UserId = userId, RoleId = adminRoleId, IsPrimary = true });
            context.Set<UserRole>().Add(new UserRole { UserId = userId, RoleId = evaluatorRoleId, IsPrimary = false });
            context.SaveChanges();
        }

        using (var context = _fixture.CreateContext())
        {
            var user = context.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role).Single(u => u.SamAccountName == "jsmith");
            Assert.Equal(2, user.UserRoles.Count);
            Assert.Contains(user.UserRoles, ur => ur.Role.Code == "admin-t3" && ur.IsPrimary);
        }
    }

    [Fact]
    public void RejectsDuplicateSamAccountName()
    {
        using var context = _fixture.CreateContext();
        context.Users.Add(new User { Id = Guid.NewGuid(), SamAccountName = "dupuser", Email = "a@gac-demo.sa", FullNameAr = "أ", FullNameEn = "A" });
        context.SaveChanges();

        context.Users.Add(new User { Id = Guid.NewGuid(), SamAccountName = "dupuser", Email = "b@gac-demo.sa", FullNameAr = "ب", FullNameEn = "B" });

        Assert.Throws<DbUpdateException>(() => context.SaveChanges());
    }
}
