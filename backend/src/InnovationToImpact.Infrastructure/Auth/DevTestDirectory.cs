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

    /// <summary> // Change 20260726
    /// A single AD-style account holding all five workflow roles at once, so a tester can exercise // Change 20260726
    /// every screen (and the in-app role switcher) without swapping identities. Named as a UPN // Change 20260726
    /// because that is the form Negotiate supplies and the form DevAuth's X-Dev-User expects here. // Change 20260726
    /// </summary> // Change 20260726
    public const string UnifiedAdminSamAccountName = "admin@internal.sa"; // Change 20260726
    public const string UnifiedAdminEmail = "admin@internal.sa"; // Change 20260726
    public const string UnifiedAdminFullNameEn = "Test All-Roles Admin"; // Change 20260726
    public const string UnifiedAdminFullNameAr = "مدير اختبار — جميع الأدوار"; // Change 20260726
    public const string UnifiedAdminDepartment = "Innovation"; // Change 20260726
    public const string UnifiedAdminTitle = "Admin"; // Change 20260726

    /// <summary>The five workflow roles granted to <see cref="UnifiedAdminSamAccountName"/>.</summary> // Change 20260726
    public static readonly IReadOnlyList<string> UnifiedAdminRoleCodes = new[] // Change 20260726
    { // Change 20260726
        RoleCodes.Admin, // Change 20260726
        RoleCodes.Supervisor, // Change 20260726
        RoleCodes.Judge, // Change 20260726
        RoleCodes.Evaluator, // Change 20260726
        RoleCodes.Submitter, // Change 20260726
    }; // Change 20260726

    /// <summary>The unified account projected as an AD identity for the dev fake directory.</summary> // Change 20260726
    public static AdIdentity UnifiedAdminAdIdentity() => new( // Change 20260726
        UnifiedAdminSamAccountName, // Change 20260726
        UnifiedAdminFullNameEn, // Change 20260726
        UnifiedAdminEmail, // Change 20260726
        UnifiedAdminDepartment, // Change 20260726
        UnifiedAdminTitle, // Change 20260726
        "devuser@gac-demo.sa"); // Change 20260726

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
            "devuser@gac-demo.sa")) // Change 20260726
            .Append(UnifiedAdminAdIdentity()); // Change 20260726

    /// <summary>All distinct role codes covered (used to grant devuser every role).</summary>
    public static IReadOnlyList<string> AllRoleCodes => Roles.Select(r => r.Code).ToList();
}
