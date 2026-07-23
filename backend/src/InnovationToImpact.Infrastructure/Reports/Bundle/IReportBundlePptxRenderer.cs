using InnovationToImpact.Domain.Reports.Bundle;

namespace InnovationToImpact.Infrastructure.Reports.Bundle;

public interface IReportBundlePptxRenderer
{
    byte[] Render(ReportBundle bundle, string locale);
}
