import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { MeApiService, BadgeSummary } from '../core/me-api.service';
import { PageHeaderComponent } from '../shared/page-header/page-header.component';
import { LoadingStateComponent } from '../shared/loading-state/loading-state.component';
import { ErrorStateComponent } from '../shared/error-state/error-state.component';

@Component({
  selector: 'app-profile-level',
  imports: [PageHeaderComponent, LoadingStateComponent, ErrorStateComponent],
  templateUrl: './profile-level.component.html',
})
export class ProfileLevelComponent implements OnInit {
  private readonly meApi = inject(MeApiService);

  readonly points = signal(0);
  readonly level = signal(1);
  readonly badges = signal<BadgeSummary[]>([]);
  readonly loading = signal(true);
  readonly loadError = signal<string | null>(null);

  readonly earnedBadges = computed(() => this.badges().filter((b) => b.earnedAt !== null));
  readonly lockedBadges = computed(() => this.badges().filter((b) => b.earnedAt === null));

  ngOnInit(): Promise<void> {
    return this.refresh();
  }

  async refresh(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(null);
    try {
      const [me, badgeResponse] = await Promise.all([this.meApi.get(), this.meApi.getBadges()]);
      this.points.set(me.points);
      this.level.set(me.level);
      this.badges.set(badgeResponse.badges);
    } catch (error) {
      this.loadError.set(
        this.extractErrorMessage(error, $localize`:@@profileLevelLoadError:Couldn't load your level. Please try again.`),
      );
    } finally {
      this.loading.set(false);
    }
  }

  private extractErrorMessage(error: unknown, fallback = $localize`Something went wrong. Please try again.`): string {
    if (error && typeof error === 'object' && 'error' in error) {
      const body = (error as { error?: { error?: string } }).error;
      if (body?.error) return body.error;
    }
    return fallback;
  }
}
