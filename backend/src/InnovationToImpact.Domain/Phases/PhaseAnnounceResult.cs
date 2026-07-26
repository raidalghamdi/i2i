namespace InnovationToImpact.Domain.Phases;

public enum PhaseAnnounceStatus
{
    Success,
    NotFound,
}

public record PhaseAnnounceResult(PhaseAnnounceStatus Status, int RecipientCount);
