import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { IdeasApiService } from '../ideas/ideas-api.service';
import { IdeaSummary } from '../ideas/idea.model';
import { MeApiService } from '../core/me-api.service';
import { NotificationsApiService, NotificationItem } from '../core/notifications-api.service';
import { PageHeaderComponent } from '../shared/page-header/page-header.component';
import { LoadingStateComponent } from '../shared/loading-state/loading-state.component';
import { EmptyStateComponent } from '../shared/empty-state/empty-state.component';
import { ErrorStateComponent } from '../shared/error-state/error-state.component';
import { StatusBadgeComponent } from '../shared/status-badge/status-badge.component';

const IN_REVIEW_STATUSES = ['submitted', 'screening', 'evaluation', 'committee', 'needs_completion'];
const ACCEPTED_STATUSES = ['approved', 'assigned', 'in_pilot', 'in_implementation', 'benefits_tracking', 'closed'];

@Component({
  selector: 'app-dashboard',
  imports: [RouterLink, DatePipe, PageHeaderComponent, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent, StatusBadgeComponent],
  templateUrl: './dashboard.component.html',
})
export class DashboardComponent implements OnInit {
  private readonly ideasApi = inject(IdeasApiService);
  private readonly meApi = inject(MeApiService);
  private readonly notificationsApi = inject(NotificationsApiService);

  readonly ideas = signal<IdeaSummary[]>([]);
  readonly level = signal(1);
  readonly points = signal(0);
  readonly notifications = signal<NotificationItem[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  readonly totalCount = computed(() => this.ideas().length);
  readonly inReviewCount = computed(() => this.ideas().filter((i) => IN_REVIEW_STATUSES.includes(i.status)).length);
  readonly acceptedCount = computed(() => this.ideas().filter((i) => ACCEPTED_STATUSES.includes(i.status)).length);
  readonly awaitingFinalize = computed(() => this.ideas().filter((i) => i.status === 'pass_awaiting_attachments'));
  readonly latest = computed(() =>
    [...this.ideas()].sort((a, b) => new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime()).slice(0, 3)
  );
  readonly recentNotifications = computed(() => this.notifications().slice(0, 3));

  async ngOnInit(): Promise<void> {
    await this.load();
  }

  reload(): void {
    void this.load();
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      const [ideas, me, notifications] = await Promise.all([
        this.ideasApi.getMine(),
        this.meApi.get(),
        this.notificationsApi.list(),
      ]);
      this.ideas.set(ideas);
      this.level.set(me.level);
      this.points.set(me.points);
      this.notifications.set(notifications);
    } catch {
      this.error.set($localize`:@@dashboardLoadError:Couldn't load your dashboard.`);
    } finally {
      this.loading.set(false);
    }
  }
}
