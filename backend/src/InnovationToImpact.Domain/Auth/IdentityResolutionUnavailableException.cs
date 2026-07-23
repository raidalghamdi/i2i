namespace InnovationToImpact.Domain.Auth;

public sealed class IdentityResolutionUnavailableException : Exception
{
    public IdentityResolutionUnavailableException(string samAccountName, Exception? innerException = null)
        : base($"Unable to resolve AD identity for '{samAccountName}'.", innerException)
    {
        SamAccountName = samAccountName;
    }

    public string SamAccountName { get; }
}
