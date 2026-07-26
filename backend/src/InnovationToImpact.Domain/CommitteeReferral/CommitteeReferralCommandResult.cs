using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.CommitteeReferral;

public sealed record CommitteeReferralCommandResult(CommitteeReferralCommandStatus Status, Idea? Idea = null);
