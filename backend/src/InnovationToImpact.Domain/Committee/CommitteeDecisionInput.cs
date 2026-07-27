namespace InnovationToImpact.Domain.Committee;

public sealed record CommitteeDecisionInput(
    string DecisionTypeCode,
    Dictionary<string, decimal> CriteriaScores,
    string? Comments,
    IReadOnlyList<CommitteeDecisionAttachmentUpload>? Attachments = null); // Change 20260726
