import { DatePipe } from '@angular/common';
import { Component, Inject, LOCALE_ID, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PublicPageHeroComponent } from '../public-page-hero/public-page-hero.component';
import { IconComponent } from '../../shared/icon/icon.component';
import { StatusBadgeComponent } from '../../shared/status-badge/status-badge.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import { PublicActivitiesApiService } from '../../core/public-activities-api.service';
import { PublicActivity } from '../../core/public-data.model';

@Component({
  selector: 'app-activities',
  imports: [
    PublicPageHeroComponent,
    IconComponent,
    StatusBadgeComponent,
    RouterLink,
    DatePipe,
    LoadingStateComponent,
    EmptyStateComponent,
    ErrorStateComponent,
  ],
  templateUrl: './activities.component.html',
})
export class ActivitiesComponent implements OnInit {
  private readonly api = inject(PublicActivitiesApiService);
  private readonly isArabic: boolean;

  readonly pageTitle = $localize`:@@activitiesTitle:Activities`;
  readonly pageBody = $localize`:@@activitiesBody:Browse the activities running throughout the competition and see how many ideas each has attracted.`;
  readonly ideasLabel = $localize`:@@activitiesIdeasCountLabel:ideas`;

  readonly activities = signal<PublicActivity[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  constructor(@Inject(LOCALE_ID) locale: string) {
    this.isArabic = locale.startsWith('ar');
  }

  async ngOnInit(): Promise<void> {
    await this.reload();
  }

  async reload(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      this.activities.set(await this.api.list());
    } catch (err) {
      this.error.set(this.extractErrorMessage(err));
    } finally {
      this.loading.set(false);
    }
  }

  name(activity: PublicActivity): string {
    return this.isArabic ? activity.nameAr : activity.nameEn;
  }

  private extractErrorMessage(error: unknown): string {
    if (error && typeof error === 'object' && 'error' in error) {
      const body = (error as { error?: { error?: string } }).error;
      if (body?.error) return body.error;
    }
    return $localize`:@@activitiesLoadError:Couldn't load activities. Please try again.`;
  }
}
