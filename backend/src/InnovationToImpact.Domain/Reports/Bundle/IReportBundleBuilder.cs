namespace InnovationToImpact.Domain.Reports.Bundle;

public interface IReportBundleBuilder
{
    Task<ReportBundle> BuildAsync(
        string type,
        DateTime? from,
        DateTime? to,
        Guid? themeId,
        string generatedBy,
        CancellationToken cancellationToken = default);
}
