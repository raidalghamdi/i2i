import { DatePipe } from '@angular/common';
import { Component, Inject, LOCALE_ID, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { StatusBadgeComponent } from '../../shared/status-badge/status-badge.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import { PublicActivitiesApiService } from '../../core/public-activities-api.service';
import { PublicActivityDetail, PublicIdeaSummary } from '../../core/public-data.model';

@Component({
  selector: 'app-activity-detail',
  imports: [RouterLink, StatusBadgeComponent, DatePipe, LoadingStateComponent, ErrorStateComponent],
  templateUrl: './activity-detail.component.html',
})
export class ActivityDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(PublicActivitiesApiService);
  private readonly isArabic: boolean;

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly detail = signal<PublicActivityDetail | null>(null);

  readonly backToActivitiesLabel = $localize`:@@activityDetailBackToActivities:Back to Activities`;
  readonly notFoundMessage = $localize`:@@activityDetailNotFound:Activity not found.`;
  readonly totalIdeasLabel = $localize`:@@activityDetailKpiTotalIdeas:Total ideas`;
  readonly approvedLabel = $localize`:@@activityDetailKpiApproved:Approved`;
  readonly pilotingLabel = $localize`:@@activityDetailKpiPiloting:Piloting`;
  readonly scoreboardTitle = $localize`:@@activityDetailScoreboardTitle:Scoreboard`;
  readonly noIdeasMessage = $localize`:@@activityDetailNoIdeas:No ideas yet.`;

  constructor(@Inject(LOCALE_ID) locale: string) {
    this.isArabic = locale.startsWith('ar');
  }

  async ngOnInit(): Promise<void> {
    await this.reload();
  }

  async reload(): Promise<void> {
    const id = this.route.snapshot.paramMap.get('id') ?? '';
    this.loading.set(true);
    this.error.set(null);
    try {
      this.detail.set(await this.api.getById(id));
    } catch (err) {
      this.error.set(this.extractErrorMessage(err));
    } finally {
      this.loading.set(false);
    }
  }

  activityName(detail: PublicActivityDetail): string {
    return this.isArabic ? detail.activity.nameAr : detail.activity.nameEn;
  }

  ideaTitle(idea: PublicIdeaSummary): string {
    return this.isArabic ? idea.titleAr : idea.titleEn;
  }

  private extractErrorMessage(error: unknown): string {
    if (error && typeof error === 'object' && 'error' in error) {
      const body = (error as { error?: { error?: string } }).error;
      if (body?.error) return body.error;
    }
    return $localize`:@@activityDetailLoadError:Couldn't load this activity. Please try again.`;
  }
}
