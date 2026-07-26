using InnovationToImpact.Domain.Auth;

namespace InnovationToImpact.Infrastructure.Auth;

/// <summary>
/// DEV/TEST ONLY. A deterministic roster of 5 users per role (40 total) used to exercise the system
/// across every role in the Development environment. These identities are added to the dev
/// <see cref="FakeAdIdentityLookupService"/> seed (so you can "log in" as them via the X-Dev-User
/// header) and are provisioned into the database as real users-with-roles by DevUsersSeeder.
/// Never runs in Production (the real LDAP lookup + JIT provisioning is used there).
///
/// Usernames follow the pattern "{roleCode}{n}", e.g. submitter1..submitter5, admin1..admin5, etc.
/// To log in as one in the running app, set it in the browser:  localStorage.setItem('devUser','judge3')  then reload.
/// </summary>
public static class DevTestDirectory
{
    // (roleCode, human-friendly title used in display names)
    private static readonly (string Code, string Title)[] Roles =
    {
        (RoleCodes.Admin, "Admin"),
        (RoleCodes.Supervisor, "Supervisor"),
        (RoleCodes.Evaluator, "Evaluator"),
        (RoleCodes.Submitter, "Submitter"),
        (RoleCodes.Judge, "Judge"),
        (RoleCodes.Expert, "Expert"),
        (RoleCodes.Mentor, "Mentor"),
        (RoleCodes.Facilitator, "Facilitator"),
    };

    private static readonly string[] Ordinals = { "One", "Two", "Three", "Four", "Five" };

    public sealed record DevTestUser(
        string SamAccountName,
        string DisplayName,
        string Email,
        string Department,
        string Title,
        string RoleCode);

    /// <summary>The 40 dev test users (5 per role).</summary>
    public static readonly IReadOnlyList<DevTestUser> Users = BuildUsers();

    private static IReadOnlyList<DevTestUser> BuildUsers()
    {
        var users = new List<DevTestUser>(Roles.Length * 5);
        foreach (var (code, title) in Roles)
        {
            for (var i = 1; i <= 5; i++)
            {
                var sam = $"{code}{i}";
                users.Add(new DevTestUser(
                    SamAccountName: sam,
                    DisplayName: $"{title} {Ordinals[i - 1]}",
                    Email: $"{sam}@gac-demo.sa",
                    Department: "Innovation",
                    Title: title,
                    RoleCode: code));
            }
        }

        return users;
    }

    /// <summary>The same roster projected as AD identities for the dev fake directory.</summary>
    public static IEnumerable<AdIdentity> AsAdIdentities() =>
        Users.Select(u => new AdIdentity(
            u.SamAccountName,
            u.DisplayName,
            u.Email,
            u.Department,
            u.Title,
            "devuser@gac-demo.sa"));

    /// <summary>All distinct role codes covered (used to grant devuser every role).</summary>
    public static IReadOnlyList<string> AllRoleCodes => Roles.Select(r => r.Code).ToList();
}
