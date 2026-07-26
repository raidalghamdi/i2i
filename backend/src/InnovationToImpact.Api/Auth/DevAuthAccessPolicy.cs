using System.Net; // Change 20260726
using System.Net.Sockets; // Change 20260726

namespace InnovationToImpact.Api.Auth; // Change 20260726

/// <summary> // Change 20260726
/// Decides whether <see cref="DevAuthenticationHandler"/> — which trusts an unauthenticated // Change 20260726
/// <c>X-Dev-User</c> header and therefore lets any caller impersonate any account — is allowed to // Change 20260726
/// handle a given request. Allowed only in Development, or when the request originates from a // Change 20260726
/// private (loopback / RFC1918) address. Everything else falls through to Negotiate (real AD). // Change 20260726
/// </summary> // Change 20260726
public static class DevAuthAccessPolicy // Change 20260726
{ // Change 20260726
    public const string DevUserHeaderName = "X-Dev-User"; // Change 20260726

    /// <summary>True when DevAuth may handle this request.</summary> // Change 20260726
    public static bool IsAllowed(HttpContext context, IWebHostEnvironment environment) => // Change 20260726
        environment.IsDevelopment() || IsPrivateAddress(ResolveClientAddress(context)); // Change 20260726

    /// <summary> // Change 20260726
    /// The address to judge the caller by. Behind a reverse proxy (Railway) the socket peer is the // Change 20260726
    /// proxy itself and always looks private, so the leftmost <c>X-Forwarded-For</c> hop — the // Change 20260726
    /// originating client — wins whenever that header is present. // Change 20260726
    /// </summary> // Change 20260726
    public static IPAddress? ResolveClientAddress(HttpContext context) // Change 20260726
    { // Change 20260726
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor)) // Change 20260726
        { // Change 20260726
            foreach (var value in forwardedFor) // Change 20260726
            { // Change 20260726
                foreach (var hop in (value ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) // Change 20260726
                { // Change 20260726
                    if (TryParseHop(hop, out var forwardedAddress)) // Change 20260726
                    { // Change 20260726
                        return forwardedAddress; // Change 20260726
                    } // Change 20260726
                } // Change 20260726
            } // Change 20260726

            // Header present but unparseable: treat the caller as untrusted rather than guessing. // Change 20260726
            return null; // Change 20260726
        } // Change 20260726

        return context.Connection.RemoteIpAddress; // Change 20260726
    } // Change 20260726

    /// <summary>Loopback, ::1, 10.0.0.0/8, 172.16.0.0/12 or 192.168.0.0/16.</summary> // Change 20260726
    public static bool IsPrivateAddress(IPAddress? address) // Change 20260726
    { // Change 20260726
        if (address is null) // Change 20260726
        { // Change 20260726
            return false; // Change 20260726
        } // Change 20260726

        if (address.IsIPv4MappedToIPv6) // Change 20260726
        { // Change 20260726
            address = address.MapToIPv4(); // Change 20260726
        } // Change 20260726

        if (IPAddress.IsLoopback(address)) // Change 20260726
        { // Change 20260726
            return true; // Change 20260726
        } // Change 20260726

        if (address.AddressFamily != AddressFamily.InterNetwork) // Change 20260726
        { // Change 20260726
            return false; // Change 20260726
        } // Change 20260726

        var octets = address.GetAddressBytes(); // Change 20260726
        return octets[0] switch // Change 20260726
        { // Change 20260726
            10 => true, // Change 20260726
            172 => octets[1] >= 16 && octets[1] <= 31, // Change 20260726
            192 => octets[1] == 168, // Change 20260726
            _ => false, // Change 20260726
        }; // Change 20260726
    } // Change 20260726

    private static bool TryParseHop(string hop, out IPAddress? address) // Change 20260726
    { // Change 20260726
        // Hops may carry a port ("203.0.113.9:41234") or be bracketed IPv6 ("[2001:db8::1]:443"). // Change 20260726
        address = null; // Change 20260726
        if (IPAddress.TryParse(hop, out var parsed)) // Change 20260726
        { // Change 20260726
            address = parsed; // Change 20260726
            return true; // Change 20260726
        } // Change 20260726

        if (IPEndPoint.TryParse(hop, out var endpoint)) // Change 20260726
        { // Change 20260726
            address = endpoint.Address; // Change 20260726
            return true; // Change 20260726
        } // Change 20260726

        return false; // Change 20260726
    } // Change 20260726
} // Change 20260726
