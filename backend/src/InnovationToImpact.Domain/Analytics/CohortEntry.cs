namespace InnovationToImpact.Domain.Analytics;

public sealed record CohortEntry(string Month, int Submitted, int Approved, int Rejected, int Implemented);
