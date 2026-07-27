namespace InnovationToImpact.Api.Auth;

public class DevAuthOptions
{
    /// <summary>
    /// Account used when a request carries no <c>X-Dev-User</c> header. Defaults to the canonical
    /// "devuser", which <c>DevUsersSeeder</c> grants every role, so an un-headered caller is
    /// authenticated as Innovator + Evaluator + Committee + Supervisor + Admin.
    /// </summary>
    public string SamAccountName { get; set; } = "devuser"; // Change 20260727

    /// <summary>
    /// TEMP: when true, the DevAuth scheme and the fake directory replace Negotiate/LDAP even in the
    /// Production environment. Enabled for the Railway test deployment; must be false for a real
    /// domain-joined install.
    /// </summary>
    public bool AlwaysOn { get; set; } // Change 20260727
}
