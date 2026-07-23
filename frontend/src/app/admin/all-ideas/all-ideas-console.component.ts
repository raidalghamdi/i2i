import { Component, Inject, LOCALE_ID, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { PageHeaderComponent } from '../../shared/page-header/page-header.component';
import { StatusBadgeComponent } from '../../shared/status-badge/status-badge.component';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';
import { IdeasApiService } from '../../ideas/ideas-api.service';
import { StrategicThemesService } from '../../ideas/strategic-themes.service';
import { ActivitiesService } from '../../ideas/activities.service';
import { SupervisorApiService } from '../../supervisor/supervisor-api.service';
import { IdeaListItem, IdeaListFilters, StrategicTheme, Activity } from '../../ideas/idea.model';

const IDEA_STATUSES = ['submitted', 'evaluation', 'returned', 'rejected', 'committee', 'approved', 'not_selected'];

@Component({
  selector: 'app-all-ideas-console',
  imports: [
    ReactiveFormsModule,
    PageHeaderComponent,
    StatusBadgeComponent,
    LoadingStateComponent,
    EmptyStateComponent,
    ErrorStateComponent,
  ],
  templateUrl: './all-ideas-console.component.html',
})
export class AllIdeasConsoleComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly ideasApi = inject(IdeasApiService);
  private readonly themesApi = inject(StrategicThemesService);
  private readonly activitiesApi = inject(ActivitiesService);
  private readonly supervisorApi = inject(SupervisorApiService);
  private readonly isArabic: boolean;

  readonly ideas = signal<IdeaListItem[]>([]);
  readonly themes = signal<StrategicTheme[]>([]);
  readonly activities = signal<Activity[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly expandedId = signal<string | null>(null);
  readonly statuses = IDEA_STATUSES;

  readonly decisionReason = signal('');
  readonly decisionError = signal<string | null>(null);
  readonly deciding = signal<string | null>(null);

  readonly filterForm = this.fb.nonNullable.group({
    q: '',
    strategicThemeId: '',
    activityId: '',
    status: '',
  });

  constructor(@Inject(LOCALE_ID) locale: string) {
    this.isArabic = locale.startsWith('ar');
  }

  async ngOnInit(): Promise<void> {
    await this.reload();
  }

  /** Full reload: re-fetches the filter lookups (themes/activities) as well as the
   * idea list. Used for the initial load and as the error-state retry action, since
   * either the lookups or the list fetch may be what failed. */
  async reload(): Promise<void> {
    this.error.set(null);
    try {
      const [, themes, activities] = await Promise.all([
        this.applyFilters(),
        this.themesApi.list(),
        this.activitiesApi.list(),
      ]);
      this.themes.set(themes);
      this.activities.set(activities);
    } catch {
      this.error.set($localize`:@@allIdeasLoadError:Couldn't load ideas. Please try again.`);
    }
  }

  async applyFilters(): Promise<void> {
    this.loading.set(true);
    this.error.set(null);
    const raw = this.filterForm.getRawValue();
    const filters: IdeaListFilters = {
      q: raw.q.trim() || undefined,
      strategicThemeId: raw.strategicThemeId || undefined,
      activityId: raw.activityId || undefined,
      status: raw.status || undefined,
      pageSize: 100,
    };
    try {
      const page = await this.ideasApi.list(filters);
      this.ideas.set(page.items);
    } catch {
      this.error.set($localize`:@@allIdeasLoadError:Couldn't load ideas. Please try again.`);
    } finally {
      this.loading.set(false);
    }
  }

  toggle(id: string): void {
    this.decisionReason.set('');
    this.decisionError.set(null);
    this.expandedId.update((cur) => (cur === id ? null : id));
  }

  title(item: IdeaListItem): string {
    return this.isArabic ? item.titleAr : item.titleEn;
  }

  problem(item: IdeaListItem): string {
    return this.isArabic ? item.problemStatementAr : item.problemStatementEn;
  }

  themeName(id: string): string {
    const t = this.themes().find((x) => x.id === id);
    return t ? (this.isArabic ? t.nameAr : t.nameEn) : '';
  }

  isArabicName(a: Activity): string {
    return this.isArabic ? a.nameAr : a.nameEn;
  }

  isDecidable(item: IdeaListItem): boolean {
    return item.status === 'submitted';
  }

  private reasonInvalid(code: string, reason: string): boolean {
    if (code === 'reject') return reason.trim().length === 0;
    if (code === 'return') return reason.trim().length < 10;
    return false;
  }

  async decide(ideaId: string, code: string): Promise<void> {
    this.decisionError.set(null);
    const reason = this.decisionReason();
    if (this.reasonInvalid(code, reason)) {
      this.decisionError.set(
        code === 'return'
          ? $localize`:@@allIdeasReasonReturn:A reason of at least 10 characters is required to return an idea.`
          : $localize`:@@allIdeasReasonReject:A reason is required to reject an idea.`,
      );
      return;
    }
    this.deciding.set(ideaId);
    try {
      await this.supervisorApi.submitScreeningDecision(ideaId, {
        decisionCode: code,
        reason: reason.trim().length > 0 ? reason : null,
      });
      this.decisionReason.set('');
      this.expandedId.set(null);
      await this.applyFilters();
    } catch (error) {
      this.decisionError.set(this.extractErrorMessage(error));
    } finally {
      this.deciding.set(null);
    }
  }

  private extractErrorMessage(error: unknown): string {
    if (error && typeof error === 'object' && 'error' in error) {
      const body = (error as { error?: { error?: string } }).error;
      if (body?.error) return body.error;
    }
    return $localize`:@@allIdeasDecisionError:Something went wrong. Please try again.`;
  }
}
