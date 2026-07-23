namespace InnovationToImpact.Domain.Briefing;

public interface IWeeklyBriefingProcessor
{
    Task<WeeklyBriefingResult> GenerateAsync(CancellationToken cancellationToken = default);
}
