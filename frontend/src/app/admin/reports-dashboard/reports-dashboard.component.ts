import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { IconComponent } from '../../shared/icon/icon.component';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { ReportsApiService } from '../reports-api.service';
import { ReportGenerationResult } from '../reports.model';
import { REPORT_CATALOG } from './report-catalog';

export type ReportFormat = 'xlsx' | 'pdf' | 'pptx';

@Component({
  selector: 'app-reports-dashboard',
  imports: [FormsModule, IconComponent, PageHeaderComponent],
  templateUrl: './reports-dashboard.component.html',
})
export class ReportsDashboardComponent {
  private readonly reportsApi = inject(ReportsApiService);

  readonly reports = REPORT_CATALOG;
  readonly errorMessage = signal<string | null>(null);
  readonly generatingKey = signal<string | null>(null);
  readonly format = signal<ReportFormat>('xlsx');

  async onGenerate(type: string): Promise<void> {
    await this.generateAndDownload(type, () => this.reportsApi.generateReport(type, { format: this.format() }));
  }

  async onExportAnalytics(): Promise<void> {
    await this.generateAndDownload('analytics', () => this.reportsApi.exportAnalytics('xlsx'));
  }

  private async generateAndDownload(key: string, generate: () => Promise<ReportGenerationResult>): Promise<void> {
    this.errorMessage.set(null);
    this.generatingKey.set(key);
    try {
      const result = await generate();
      if (result.status !== 'completed') {
        this.errorMessage.set($localize`Report generation failed. Please try again.`);
        return;
      }
      const blob = await this.reportsApi.downloadReport(result.reportGenerationId);
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement('a');
      anchor.href = url;
      anchor.download = key === 'analytics' ? `${key}.xlsx` : `${key}.${this.format()}`;
      anchor.click();
      URL.revokeObjectURL(url);
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
    } finally {
      this.generatingKey.set(null);
    }
  }

  private extractErrorMessage(error: unknown): string {
    if (error && typeof error === 'object' && 'error' in error) {
      const body = (error as { error?: { error?: string } }).error;
      if (body?.error) return body.error;
    }
    return $localize`Something went wrong. Please try again.`;
  }
}
