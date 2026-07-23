import { DecimalPipe } from '@angular/common';
import { Component, Inject, LOCALE_ID, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AnalyticsApiService } from '../../admin/analytics-api.service';
import { ExecutiveAnalytics, TopObjectiveEntry } from '../../admin/analytics.model';
import { FunnelChartComponent, FunnelDatum } from '../../shared/charts/funnel-chart/funnel-chart.component';
import { IconComponent } from '../../shared/icon/icon.component';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';

const FUNNEL_LABELS: Record<string, string> = {
  Participation: $localize`:@@analyticsOverviewFunnelParticipation:Participation`,
  Evaluated: $localize`:@@analyticsOverviewFunnelEvaluated:Evaluated`,
  Approved: $localize`:@@analyticsOverviewFunnelApproved:Approved`,
  Piloted: $localize`:@@analyticsOverviewFunnelPiloted:Piloted`,
  Scaled: $localize`:@@analyticsOverviewFunnelScaled:Scaled`,
};

/** Executive analytics overview: `/analytics`. Entry point into the per-theme
 * pillar drill-down (`/analytics/pillars/:themeId`) via the top-objectives list. */
@Component({
  selector: 'app-analytics-overview',
  imports: [DecimalPipe, RouterLink, PageHeaderComponent, IconComponent, FunnelChartComponent, LoadingStateComponent, ErrorStateComponent],
  templateUrl: './analytics-overview.component.html',
})
export class AnalyticsOverviewComponent implements OnInit {
  private readonly analyticsApi = inject(AnalyticsApiService);
  private readonly isArabic: boolean;

  readonly exec = signal<ExecutiveAnalytics | null>(null);
  readonly loading = signal(true);
  readonly loadError = signal<string | null>(null);

  readonly funnelRows = computed<FunnelDatum[]>(() =>
    (this.exec()?.funnel ?? []).map((entry) => ({
      label: FUNNEL_LABELS[entry.stageKey] ?? entry.stageKey,
      count: entry.count,
    })),
  );

  constructor(@Inject(LOCALE_ID) locale: string) {
    this.isArabic = locale.startsWith('ar');
  }

  ngOnInit(): Promise<void> {
    return this.refresh();
  }

  async refresh(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(null);
    try {
      this.exec.set(await this.analyticsApi.getExecutive());
    } catch (error) {
      this.loadError.set(
        this.extractErrorMessage(error, $localize`:@@analyticsOverviewLoadError:Couldn't load analytics. Please try again.`),
      );
    } finally {
      this.loading.set(false);
    }
  }

  objectiveName(obj: TopObjectiveEntry): string {
    return this.isArabic ? obj.nameAr : obj.nameEn;
  }

  objectiveWidthPct(obj: TopObjectiveEntry): number {
    const rows = this.exec()?.topObjectives ?? [];
    const max = Math.max(...rows.map((r) => r.count), 1);
    return Math.max((obj.count / max) * 100, 6);
  }

  private extractErrorMessage(error: unknown, fallback = $localize`Something went wrong. Please try again.`): string {
    if (error && typeof error === 'object' && 'error' in error) {
      const body = (error as { error?: { error?: string } }).error;
      if (body?.error) return body.error;
    }
    return fallback;
  }
}
