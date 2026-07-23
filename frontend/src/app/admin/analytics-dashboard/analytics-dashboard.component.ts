import { DecimalPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { IconComponent } from '../../shared/icon/icon.component';
import { ExportBarComponent } from '../../shared/export-bar/export-bar.component';
import { IdeasByStageChartComponent } from '../../shared/charts/ideas-by-stage-chart/ideas-by-stage-chart.component';
import { SubmissionsLineChartComponent } from '../../shared/charts/submissions-line-chart/submissions-line-chart.component';
import { CohortChartComponent } from '../../shared/charts/cohort-chart/cohort-chart.component';
import { FunnelChartComponent, FunnelDatum } from '../../shared/charts/funnel-chart/funnel-chart.component';
import { AvgTimePerStageTableComponent } from '../../shared/charts/avg-time-per-stage-table/avg-time-per-stage-table.component';
import { ConversionStatCardComponent } from '../../shared/charts/conversion-stat-card/conversion-stat-card.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import { AnalyticsApiService } from '../analytics-api.service';
import { AnalyticsDashboard, ExecutiveAnalytics } from '../analytics.model';

const FUNNEL_LABELS: Record<string, string> = {
  Participation: $localize`:@@analyticsFunnelParticipation:Participation`,
  Evaluated: $localize`:@@analyticsFunnelEvaluated:Evaluated`,
  Approved: $localize`:@@analyticsFunnelApproved:Approved`,
  Piloted: $localize`:@@analyticsFunnelPiloted:Piloted`,
  Scaled: $localize`:@@analyticsFunnelScaled:Scaled`,
};

@Component({
  selector: 'app-analytics-dashboard',
  imports: [
    DecimalPipe,
    PageHeaderComponent,
    IconComponent,
    ExportBarComponent,
    IdeasByStageChartComponent,
    SubmissionsLineChartComponent,
    CohortChartComponent,
    FunnelChartComponent,
    AvgTimePerStageTableComponent,
    ConversionStatCardComponent,
    LoadingStateComponent,
    ErrorStateComponent,
  ],
  templateUrl: './analytics-dashboard.component.html',
})
export class AnalyticsDashboardComponent implements OnInit {
  private readonly analyticsApi = inject(AnalyticsApiService);

  readonly dashboard = signal<AnalyticsDashboard | null>(null);
  readonly exec = signal<ExecutiveAnalytics | null>(null);
  readonly loading = signal(true);
  readonly errorMessage = signal<string | null>(null);

  readonly funnelRows = computed<FunnelDatum[]>(() =>
    (this.exec()?.funnel ?? []).map((entry) => ({
      label: FUNNEL_LABELS[entry.stageKey] ?? entry.stageKey,
      count: entry.count,
    })),
  );

  async ngOnInit(): Promise<void> {
    await this.reload();
  }

  async reload(): Promise<void> {
    this.errorMessage.set(null);
    this.loading.set(true);
    try {
      const [dashboard, exec] = await Promise.all([this.analyticsApi.getDashboard(), this.analyticsApi.getExecutive()]);
      this.dashboard.set(dashboard);
      this.exec.set(exec);
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
    } finally {
      this.loading.set(false);
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
