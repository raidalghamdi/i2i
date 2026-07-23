using InnovationToImpact.Domain.Audit;
using Xunit;

namespace InnovationToImpact.Infrastructure.Tests;

public class AuditHashComputerTests
{
    private static readonly DateTime FixedOccurredAt = new(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid FixedEntityId = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid FixedActorId = new("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void ProducesA64CharacterLowercaseHexDigest()
    {
        var hash = AuditHashComputer.ComputeHash(null, 1, "idea", FixedEntityId, "create", FixedActorId, "{}", FixedOccurredAt);

        Assert.Equal(64, hash.Length);
        Assert.Equal(hash, hash.ToLowerInvariant());
        Assert.Matches("^[0-9a-f]{64}$", hash);
    }

    [Fact]
    public void IsDeterministicForIdenticalInput()
    {
        var hash1 = AuditHashComputer.ComputeHash("prev", 5, "idea", FixedEntityId, "update", FixedActorId, "{\"x\":1}", FixedOccurredAt);
        var hash2 = AuditHashComputer.ComputeHash("prev", 5, "idea", FixedEntityId, "update", FixedActorId, "{\"x\":1}", FixedOccurredAt);

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void DiffersWhenPrevHashDiffers()
    {
        var hashA = AuditHashComputer.ComputeHash("prev-a", 5, "idea", FixedEntityId, "update", FixedActorId, "{}", FixedOccurredAt);
        var hashB = AuditHashComputer.ComputeHash("prev-b", 5, "idea", FixedEntityId, "update", FixedActorId, "{}", FixedOccurredAt);

        Assert.NotEqual(hashA, hashB);
    }

    [Fact]
    public void DiffersWhenPayloadDiffers()
    {
        var hashA = AuditHashComputer.ComputeHash(null, 1, "idea", FixedEntityId, "create", null, "{\"a\":1}", FixedOccurredAt);
        var hashB = AuditHashComputer.ComputeHash(null, 1, "idea", FixedEntityId, "create", null, "{\"a\":2}", FixedOccurredAt);

        Assert.NotEqual(hashA, hashB);
    }

    [Fact]
    public void HandlesNullPrevHashActorIdAndPayloadWithoutThrowing()
    {
        var hash = AuditHashComputer.ComputeHash(null, 1, "idea", FixedEntityId, "create", null, null, FixedOccurredAt);

        Assert.Equal(64, hash.Length);
    }

    [Fact]
    public void IsInvariantToDateTimeKind()
    {
        // Regression test for the OccurredAt/DateTimeKind bug found during the final
        // whole-branch review: EF Core reads DateTime columns back as Kind=Unspecified
        // regardless of what was written, so the hash must not depend on Kind at all.
        var utc = new DateTime(FixedOccurredAt.Ticks, DateTimeKind.Utc);
        var unspecified = new DateTime(FixedOccurredAt.Ticks, DateTimeKind.Unspecified);

        var hashUtc = AuditHashComputer.ComputeHash(null, 1, "idea", FixedEntityId, "create", null, "{}", utc);
        var hashUnspecified = AuditHashComputer.ComputeHash(null, 1, "idea", FixedEntityId, "create", null, "{}", unspecified);

        Assert.Equal(hashUtc, hashUnspecified);
    }
}
