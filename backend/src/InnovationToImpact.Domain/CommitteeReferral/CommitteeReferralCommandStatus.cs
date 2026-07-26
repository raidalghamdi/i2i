namespace InnovationToImpact.Domain.CommitteeReferral;

public enum CommitteeReferralCommandStatus
{
    Success,
    NotFound,
    InvalidState,
    JudgesRequired,
    InvalidJudge,
}
