namespace InnovationToImpact.Infrastructure.Auth;

public class ActiveDirectoryOptions
{
    public string Domain { get; set; } = string.Empty;
    public string ServiceAccountUsername { get; set; } = string.Empty;
    public string ServiceAccountPassword { get; set; } = string.Empty;
    public int CacheTtlMinutes { get; set; } = 60;
}
