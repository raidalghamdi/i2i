namespace InnovationToImpact.Domain.CommitteeReferral;

public sealed record SubmitToCommitteeInput(IReadOnlyList<Guid> JudgeIds);
