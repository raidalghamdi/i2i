using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace InnovationToImpact.Api.Auth;

public class DevAuthenticationOptions : AuthenticationSchemeOptions
{
}

public static class DevAuthenticationDefaults
{
    public const string AuthenticationScheme = "DevAuth";
}

public class DevAuthenticationHandler : AuthenticationHandler<DevAuthenticationOptions>
{
    private readonly DevAuthOptions _devAuthOptions;

    public DevAuthenticationHandler(
        IOptionsMonitor<DevAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptions<DevAuthOptions> devAuthOptions)
        : base(options, logger, encoder)
    {
        _devAuthOptions = devAuthOptions.Value;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var samAccountName = Request.Headers.TryGetValue("X-Dev-User", out var headerValue) && !string.IsNullOrWhiteSpace(headerValue)
            ? headerValue.ToString()
            : _devAuthOptions.SamAccountName;

        if (string.IsNullOrWhiteSpace(samAccountName))
        {
            return Task.FromResult(AuthenticateResult.Fail(
                "No dev identity configured. Set DevAuth:SamAccountName or the X-Dev-User header."));
        }

        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, samAccountName) }, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
