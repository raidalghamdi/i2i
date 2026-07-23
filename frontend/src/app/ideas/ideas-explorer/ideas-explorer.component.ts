import { Component, Inject, LOCALE_ID, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Subject, Subscription } from 'rxjs';
import { debounceTime } from 'rxjs/operators';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { StatusBadgeComponent } from '../../shared/status-badge/status-badge.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import { IdeasApiService } from '../ideas-api.service';
import { StrategicThemesService } from '../strategic-themes.service';
import { ActivitiesService } from '../activities.service';
import { Activity, IdeaListItem, StrategicTheme } from '../idea.model';

export const IDEA_STATUS_CODES = [
  'draft',
  'submitted',
  'evaluation',
  'pass_awaiting_attachments',
  'evaluation_failed',
  'committee',
  'pending_final_ranking',
  'rejected',
  'returned',
  'approved',
  'not_selected',
  'in_pilot',
  'in_measurement',
  'in_scaling',
];

export const IDEA_STAGES = [0, 1, 2, 3, 4, 5, 6, 7, 8];

@Component({
  selector: 'app-ideas-explorer',
  imports: [
    FormsModule,
    RouterLink,
    PageHeaderComponent,
    StatusBadgeComponent,
    LoadingStateComponent,
    EmptyStateComponent,
    ErrorStateComponent,
  ],
  templateUrl: './ideas-explorer.component.html',
})
export class IdeasExplorerComponent implements OnInit, OnDestroy {
  private readonly ideasApi = inject(IdeasApiService);
  private readonly themesApi = inject(StrategicThemesService);
  private readonly activitiesApi = inject(ActivitiesService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly isArabic: boolean;

  private readonly searchInput$ = new Subject<string>();
  private searchSub?: Subscription;

  readonly statusCodes = IDEA_STATUS_CODES;
  readonly stages = IDEA_STAGES;

  readonly items = signal<IdeaListItem[]>([]);
  readonly total = signal(0);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly themes = signal<StrategicTheme[]>([]);
  readonly activities = signal<Activity[]>([]);

  readonly q = signal('');
  readonly themeId = signal('');
  readonly activityId = signal('');
  readonly status = signal('');
  readonly stage = signal<number | ''>('');

  constructor(@Inject(LOCALE_ID) locale: string) {
    this.isArabic = locale.startsWith('ar');
  }

  async ngOnInit(): Promise<void> {
    const params = this.route.snapshot.queryParamMap;
    this.q.set(params.get('q') ?? '');
    this.themeId.set(params.get('themeId') ?? '');
    this.activityId.set(params.get('activityId') ?? '');
    this.status.set(params.get('status') ?? '');
    const stageParam = params.get('stage');
    this.stage.set(stageParam !== null && stageParam !== '' ? Number(stageParam) : '');

    this.searchSub?.unsubscribe();
    this.searchSub = this.searchInput$.pipe(debounceTime(300)).subscribe(() => {
      void this.onFilterChange();
    });

    await this.reload();
  }

  ngOnDestroy(): void {
    this.searchSub?.unsubscribe();
  }

  onSearchInput(value: string): void {
    this.q.set(value);
    this.searchInput$.next(value);
  }

  /** Full reload: re-fetches the filter lookups (themes/activities) as well as the
   * idea list. Used for the initial load and as the error-state retry action, since
   * either the lookups or the list fetch may be what failed. */
  async reload(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      const [themes, activities] = await Promise.all([this.themesApi.list(), this.activitiesApi.list()]);
      this.themes.set(themes);
      this.activities.set(activities);

      const page = await this.ideasApi.list(this.currentFilters());
      this.items.set(page.items);
      this.total.set(page.total);
    } catch {
      this.error.set($localize`:@@ideasExplorerLoadError:Couldn't load ideas. Please try again.`);
    } finally {
      this.loading.set(false);
    }

    this.navigateWithFilters();
  }

  async onFilterChange(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    try {
      const page = await this.ideasApi.list(this.currentFilters());
      this.items.set(page.items);
      this.total.set(page.total);
    } catch {
      this.error.set($localize`:@@ideasExplorerLoadError:Couldn't load ideas. Please try again.`);
    } finally {
      this.loading.set(false);
    }

    this.navigateWithFilters();
  }

  private currentFilters() {
    return {
      q: this.q() || undefined,
      strategicThemeId: this.themeId() || undefined,
      activityId: this.activityId() || undefined,
      status: this.status() || undefined,
      stage: this.stage() !== '' ? Number(this.stage()) : undefined,
    };
  }

  private navigateWithFilters(): void {
    void this.router.navigate(['/ideas'], {
      queryParams: {
        q: this.q() || null,
        themeId: this.themeId() || null,
        activityId: this.activityId() || null,
        status: this.status() || null,
        stage: this.stage() !== '' ? Number(this.stage()) : null,
      },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }

  title(item: IdeaListItem): string {
    return this.isArabic ? item.titleAr : item.titleEn;
  }

  problem(item: IdeaListItem): string {
    return this.isArabic ? item.problemStatementAr : item.problemStatementEn;
  }

  themeName(theme: StrategicTheme): string {
    return this.isArabic ? theme.nameAr : theme.nameEn;
  }

  activityName(activity: Activity): string {
    return this.isArabic ? activity.nameAr : activity.nameEn;
  }
}
