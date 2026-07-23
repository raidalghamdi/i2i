namespace InnovationToImpact.Domain.Sla;

public sealed record SlaScanResult(int Scanned, int NewlyBreached, int ApproachingBreach, IReadOnlyList<Guid> NewlyBreachedTrackingIds);
