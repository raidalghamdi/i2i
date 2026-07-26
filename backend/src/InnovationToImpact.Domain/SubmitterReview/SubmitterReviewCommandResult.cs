using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.SubmitterReview;

public sealed record SubmitterReviewCommandResult(SubmitterReviewCommandStatus Status, Idea? Idea = null);
