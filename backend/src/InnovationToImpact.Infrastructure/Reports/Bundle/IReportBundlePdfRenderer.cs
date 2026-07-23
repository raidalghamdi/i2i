using InnovationToImpact.Domain.Reports.Bundle;

namespace InnovationToImpact.Infrastructure.Reports.Bundle;

public interface IReportBundlePdfRenderer
{
    byte[] Render(ReportBundle bundle, string locale);
}
