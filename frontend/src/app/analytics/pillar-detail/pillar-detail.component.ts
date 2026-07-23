import { DecimalPipe } from '@angular/common';
import { Component, Inject, LOCALE_ID, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { AnalyticsApiService } from '../../admin/analytics-api.service';
import { PillarDetail, PillarIdeaRow } from '../../admin/analytics.model';
import { SubmissionsDatum, SubmissionsLineChartComponent } from '../../shared/charts/submissions-line-chart/submissions-line-chart.component';
import { IconComponent } from '../../shared/icon/icon.component';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { StatusBadgeComponent } from '../../shared/status-badge/status-badge.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';

/** Per-theme drill-down: `/analytics/pillars/:themeId`. Entry point is the
 * top-objectives list on the executive overview (`/analytics`).
 *
 * Note: `AnalyticsApiService.getPillar` already swallows fetch errors into a
 * `null` result (`catchError(() => of(null))`), so a genuine network/API
 * failure is indistinguishable from a real "pillar not found" at this layer
 * — there's no `app-error-state` here because there's nothing to retry that
 * the service itself wouldn't already have retried into `null`. */
@Component({
  selector: 'app-pillar-detail',
  imports: [DecimalPipe, PageHeaderComponent, IconComponent, StatusBadgeComponent, SubmissionsLineChartComponent, LoadingStateComponent],
  templateUrl: './pillar-detail.component.html',
})
export class PillarDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly analyticsApi = inject(AnalyticsApiService);
  private readonly isArabic: boolean;

  readonly detail = signal<PillarDetail | null>(null);
  readonly loading = signal(true);

  readonly name = computed(() => {
    const d = this.detail();
    if (!d) return '';
    return this.isArabic ? d.nameAr : d.nameEn;
  });

  readonly description = computed(() => {
    const d = this.detail();
    if (!d) return '';
    return this.isArabic ? d.descriptionAr : d.descriptionEn;
  });

  readonly timelineAsSubmissions = computed<SubmissionsDatum[]>(() =>
    (this.detail()?.timeline ?? []).map((entry) => ({ date: entry.month, count: entry.count })),
  );

  constructor(@Inject(LOCALE_ID) locale: string) {
    this.isArabic = locale.startsWith('ar');
  }

  async ngOnInit(): Promise<void> {
    const themeId = this.route.snapshot.paramMap.get('themeId') ?? '';
    this.loading.set(true);
    try {
      this.detail.set(await this.analyticsApi.getPillar(themeId));
    } finally {
      this.loading.set(false);
    }
  }

  ideaTitle(idea: PillarIdeaRow): string {
    return this.isArabic ? idea.titleAr : idea.titleEn;
  }
}
