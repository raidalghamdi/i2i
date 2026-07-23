import { Component, inject, signal } from '@angular/core';
import { AnalyticsApiService } from '../../admin/analytics-api.service';

export type ExportFormat = 'pdf' | 'pptx' | 'xlsx';

/** Header action bar for the executive analytics dashboard: exports the
 * current analytics snapshot as PDF/PPTX/XLSX and downloads the result. */
@Component({
  selector: 'app-export-bar',
  templateUrl: './export-bar.component.html',
})
export class ExportBarComponent {
  private readonly analyticsApi = inject(AnalyticsApiService);

  readonly errorMessage = signal<string | null>(null);
  readonly exportingFormat = signal<ExportFormat | null>(null);

  async onExportPdf(): Promise<void> {
    await this.exportAndDownload('pdf');
  }

  async onExportPptx(): Promise<void> {
    await this.exportAndDownload('pptx');
  }

  async onExportXlsx(): Promise<void> {
    await this.exportAndDownload('xlsx');
  }

  private async exportAndDownload(format: ExportFormat): Promise<void> {
    this.errorMessage.set(null);
    this.exportingFormat.set(format);
    try {
      const result = await this.analyticsApi.exportAnalytics(format);
      if (result.status !== 'completed') {
        this.errorMessage.set($localize`:@@exportBarError:Export failed. Please try again.`);
        return;
      }
      const blob = await this.analyticsApi.downloadReport(result.reportGenerationId);
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement('a');
      anchor.href = url;
      anchor.download = `analytics.${format}`;
      anchor.click();
      URL.revokeObjectURL(url);
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
    } finally {
      this.exportingFormat.set(null);
    }
  }

  private extractErrorMessage(error: unknown): string {
    if (error && typeof error === 'object' && 'error' in error) {
      const body = (error as { error?: { error?: string } }).error;
      if (body?.error) return body.error;
    }
    return $localize`:@@exportBarGenericError:Something went wrong. Please try again.`;
  }
}
