using InnovationToImpact.Domain.Auth;
using InnovationToImpact.Domain.Entities;
using InnovationToImpact.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InnovationToImpact.Infrastructure.Auth;

/// <summary>
/// DEV/TEST ONLY seeder. Provisions the <see cref="DevTestDirectory"/> roster (5 users per role, 40
/// total) as real users-with-roles, and grants the canonical "devuser" every role so all roles can be
/// exercised via the in-app role switcher. Idempotent — only creates users / role assignments that are
/// missing. Must be gated to the Development environment by the caller; never run in Production.
/// </summary>
public static class DevUsersSeeder
{
    public static async Task SeedAsync(InnovationDbContext db, CancellationToken cancellationToken = default)
    {
        var rolesByCode = await db.Roles.ToDictionaryAsync(r => r.Code, r => r, StringComparer.OrdinalIgnoreCase, cancellationToken);
        if (rolesByCode.Count == 0)
        {
            // Canonical roles not seeded yet — nothing to attach; skip quietly.
            return;
        }

        var wantedSams = DevTestDirectory.Users.Select(u => u.SamAccountName)
            .Append("devuser")
            .ToList();

        var existing = await db.Users
            .Include(u => u.UserRoles)
            .Where(u => wantedSams.Contains(u.SamAccountName))
            .ToDictionaryAsync(u => u.SamAccountName, u => u, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var changed = false;

        // 40 role-specific test users.
        foreach (var testUser in DevTestDirectory.Users)
        {
            if (!rolesByCode.TryGetValue(testUser.RoleCode, out var role))
            {
                continue;
            }

            if (!existing.TryGetValue(testUser.SamAccountName, out var user))
            {
                user = new User
                {
                    Id = Guid.NewGuid(),
                    SamAccountName = testUser.SamAccountName,
                    Email = testUser.Email,
                    FullNameAr = testUser.DisplayName,
                    FullNameEn = testUser.DisplayName,
                    Department = testUser.Department,
                    Title = testUser.Title,
                    ManagerEmail = "devuser@gac-demo.sa",
                    IsActive = true,
                };
                db.Users.Add(user);
                existing[testUser.SamAccountName] = user;
                changed = true;
            }

            if (user.UserRoles.All(ur => ur.RoleId != role.Id))
            {
                user.UserRoles.Add(new UserRole
                {
                    UserId = user.Id,
                    RoleId = role.Id,
                    // Their own role is primary if they have no primary yet.
                    IsPrimary = user.UserRoles.All(ur => !ur.IsPrimary),
                });
                changed = true;
            }
        }

        // devuser gets every role so the role switcher can reach all of them.
        if (!existing.TryGetValue("devuser", out var dev))
        {
            dev = new User
            {
                Id = Guid.NewGuid(),
                SamAccountName = "devuser",
                Email = "devuser@gac-demo.sa",
                FullNameAr = "Dev User",
                FullNameEn = "Dev User",
                Department = "Innovation",
                Title = "Software Engineer",
                ManagerEmail = "manager@gac-demo.sa",
                IsActive = true,
            };
            db.Users.Add(dev);
            existing["devuser"] = dev;
            changed = true;
        }

        foreach (var code in DevTestDirectory.AllRoleCodes)
        {
            if (!rolesByCode.TryGetValue(code, out var role))
            {
                continue;
            }

            if (dev.UserRoles.All(ur => ur.RoleId != role.Id))
            {
                dev.UserRoles.Add(new UserRole
                {
                    UserId = dev.Id,
                    RoleId = role.Id,
                    IsPrimary = code == RoleCodes.Admin && dev.UserRoles.All(ur => !ur.IsPrimary),
                });
                changed = true;
            }
        }

        if (changed)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
