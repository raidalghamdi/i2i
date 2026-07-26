using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.Committee;

public sealed record CommitteeQueueItem(Idea Idea, bool HasDecided, int DecidedCount, int TotalJudges);
