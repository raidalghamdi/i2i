using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace InnovationToImpact.Domain.Audit;

public static class AuditHashComputer
{
    public static string ComputeHash(
        string? prevHash,
        long chainSeq,
        string entityType,
        Guid entityId,
        string action,
        Guid? actorId,
        string? payload,
        DateTime occurredAt)
    {
        // OccurredAt is serialized as Ticks, NOT a round-trip ("o") format string. "o" embeds
        // DateTimeKind (Utc renders a trailing Z, Unspecified renders none), but EF Core
        // materializes DateTime columns back from SQL Server and SQLite as Unspecified
        // regardless of what was written -- so a verifier reading a row back from a fresh
        // DbContext would compute a different OccurredAt string than the writer did for the
        // identical instant, breaking verification for every genuine chain. Ticks carries no
        // Kind metadata and is immune to this.
        var input = string.Join('|',
            prevHash ?? string.Empty,
            chainSeq.ToString(CultureInfo.InvariantCulture),
            entityType,
            entityId.ToString(),
            action,
            actorId?.ToString() ?? string.Empty,
            payload ?? string.Empty,
            occurredAt.Ticks.ToString(CultureInfo.InvariantCulture));

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
