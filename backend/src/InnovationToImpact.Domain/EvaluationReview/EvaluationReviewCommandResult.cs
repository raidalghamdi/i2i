using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.EvaluationReview;

public sealed record EvaluationReviewCommandResult(EvaluationReviewCommandStatus Status, Idea? Idea = null);
