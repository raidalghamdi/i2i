using InnovationToImpact.Domain.Reports.Bundle;

namespace InnovationToImpact.Infrastructure.Reports.Bundle;

public interface IReportBundleXlsxRenderer
{
    byte[] Render(ReportBundle bundle, string locale);
}
