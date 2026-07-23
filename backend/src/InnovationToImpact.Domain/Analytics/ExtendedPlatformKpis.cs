namespace InnovationToImpact.Domain.Analytics;

public sealed record ExtendedPlatformKpis(
    int TotalSubmissions,
    int TotalApproved,
    int TotalImplemented,
    int ActiveSubmitters,
    int TotalEvaluations,
    int TotalUsers,
    int TotalEvaluators,
    decimal RealizedFinancialImpact);
