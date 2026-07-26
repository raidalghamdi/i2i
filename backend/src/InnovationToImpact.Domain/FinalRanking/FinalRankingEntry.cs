namespace InnovationToImpact.Domain.FinalRanking;

public sealed record FinalRankingEntry(Guid IdeaId, string Code, string TitleEn, Guid TrackId, int Rank, decimal? Score, string OutcomeStatus);
