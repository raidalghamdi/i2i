import { DatePipe } from '@angular/common';
import { Component, Inject, LOCALE_ID, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { StatusBadgeComponent } from '../../shared/status-badge/status-badge.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import { IdeasApiService } from '../ideas-api.service';
import { MyIdeaItem } from '../idea.model';
import { PioneerBadgeComponent } from '../pioneer-badge/pioneer-badge.component';
import { FeedbackCountBadgeComponent } from '../feedback-count-badge/feedback-count-badge.component';

const WITHDRAWABLE_STATUSES = new Set(['draft', 'submitted', 'returned']);

@Component({
  selector: 'app-my-ideas',
  imports: [
    DatePipe,
    RouterLink,
    PageHeaderComponent,
    StatusBadgeComponent,
    PioneerBadgeComponent,
    FeedbackCountBadgeComponent,
    LoadingStateComponent,
    EmptyStateComponent,
    ErrorStateComponent,
  ],
  templateUrl: './my-ideas.component.html',
})
export class MyIdeasComponent implements OnInit {
  private readonly ideasApi = inject(IdeasApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly isArabic: boolean;

  readonly items = signal<MyIdeaItem[]>([]);
  readonly statusGroup = signal<string>('');
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly withdrawError = signal<string | null>(null);

  constructor(@Inject(LOCALE_ID) locale: string) {
    this.isArabic = locale.startsWith('ar');
  }

  async ngOnInit(): Promise<void> {
    const raw = this.route.snapshot.queryParamMap.get('status') ?? '';
    this.statusGroup.set(raw);
    await this.reload();
  }

  async reload(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      this.items.set(await this.ideasApi.getMineDetailed(this.statusGroup() || undefined));
    } catch {
      this.error.set($localize`:@@myIdeasLoadError:Couldn't load your ideas. Please try again.`);
    } finally {
      this.loading.set(false);
    }
  }

  async onChipClick(group: string): Promise<void> {
    this.statusGroup.set(group);
    await this.reload();
  }

  async onWithdraw(id: string): Promise<void> {
    this.withdrawError.set(null);
    try {
      await this.ideasApi.withdraw(id);
      await this.reload();
    } catch {
      this.withdrawError.set(
        $localize`:@@myIdeasWithdrawError:Could not withdraw the idea. Please try again.`,
      );
    }
  }

  title(item: MyIdeaItem): string {
    return this.isArabic ? item.titleAr : item.titleEn;
  }

  isWithdrawable(item: MyIdeaItem): boolean {
    return WITHDRAWABLE_STATUSES.has(item.status);
  }
}
