namespace InnovationToImpact.Domain.FinalRanking;

public sealed record FinalRankingResult(int ApprovedCount, int NotSelectedCount, int TopN, IReadOnlyList<FinalRankingEntry> Entries);
