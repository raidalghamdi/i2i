namespace InnovationToImpact.Domain.Entities;

public class Pilot
{
    public Guid Id { get; set; }

    public Guid IdeaId { get; set; }
    public Idea Idea { get; set; } = null!;

    public string HypothesisAr { get; set; } = string.Empty;
    public string HypothesisEn { get; set; } = string.Empty;
    public string ExperimentPlanAr { get; set; } = string.Empty;
    public string ExperimentPlanEn { get; set; } = string.Empty;

    public decimal Budget { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public string? MilestonesJson { get; set; }
    public string? ResultsAr { get; set; }
    public string? ResultsEn { get; set; }
    public string? LessonsLearnedAr { get; set; }
    public string? LessonsLearnedEn { get; set; }

    public Guid PilotStatusId { get; set; }
    public PilotStatus PilotStatus { get; set; } = null!;
}
