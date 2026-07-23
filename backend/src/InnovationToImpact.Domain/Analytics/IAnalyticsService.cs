namespace InnovationToImpact.Domain.Analytics;

public record PillarKpis(int Ideas, decimal BudgetSpent, decimal BudgetAllocated, int PilotsActive, int ImplementationsDone);

public record PillarTimelineEntry(string Month, int Count);

public record PillarIdeaRow(Guid Id, string Code, string TitleAr, string TitleEn, string Status, int CurrentStage);

public record PillarDetail(
    Guid ThemeId,
    string NameAr,
    string NameEn,
    string? DescriptionAr,
    string? DescriptionEn,
    string? OwnerName,
    PillarKpis Kpis,
    IReadOnlyList<PillarTimelineEntry> Timeline,
    IReadOnlyList<PillarIdeaRow> Ideas);

public interface IAnalyticsService
{
    Task<PlatformKpis> GetPlatformKpisAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IdeasByStatusEntry>> GetIdeasByStatusAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SubmissionsOverTimeEntry>> GetSubmissionsOverTimeAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ThemeActivityEntry>> GetThemeActivityAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TopEvaluatorEntry>> GetTopEvaluatorsAsync(CancellationToken cancellationToken = default);
    Task<SlaComplianceResult> GetSlaComplianceAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FunnelEntry>> GetFunnelAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CohortEntry>> GetCohortAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<IdeasByStageEntry>> GetIdeasByStageAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SubmissionsOverTimeEntry>> GetSubmissionsOverTimeFilledAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TopObjectiveEntry>> GetTopObjectivesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AvgTimePerStageEntry>> GetAvgTimePerStageAsync(CancellationToken cancellationToken = default);
    Task<ConversionResult> GetConversionAsync(CancellationToken cancellationToken = default);
    Task<ExtendedPlatformKpis> GetExtendedPlatformKpisAsync(CancellationToken cancellationToken = default);

    Task<PillarDetail?> GetPillarDetailAsync(Guid themeId, CancellationToken cancellationToken = default);
}
