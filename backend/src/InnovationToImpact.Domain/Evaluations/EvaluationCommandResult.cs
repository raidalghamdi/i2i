using InnovationToImpact.Domain.Entities;

namespace InnovationToImpact.Domain.Evaluations;

public sealed record EvaluationCommandResult(EvaluationCommandStatus Status, Evaluation? Evaluation = null, Idea? Idea = null);
