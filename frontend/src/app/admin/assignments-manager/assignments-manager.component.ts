import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AssignmentApiService } from '../assignment-api.service';
import { Assignment, AssignmentCreateInput, IdeaOption } from '../assignment.model';
import { SupervisorApiService } from '../../supervisor/supervisor-api.service';
import { RoleUser } from '../../supervisor/supervisor.model';
import { LoadingStateComponent } from '../../shared/loading-state/loading-state.component';
import { EmptyStateComponent } from '../../shared/empty-state/empty-state.component';
import { ErrorStateComponent } from '../../shared/error-state/error-state.component';

interface CreateDraft {
  ideaId: string;
  evaluatorId: string;
  dueAt: string;
  notes: string;
}

const EMPTY_DRAFT: CreateDraft = { ideaId: '', evaluatorId: '', dueAt: '', notes: '' };

@Component({
  selector: 'app-assignments-manager',
  imports: [FormsModule, DatePipe, LoadingStateComponent, EmptyStateComponent, ErrorStateComponent],
  templateUrl: './assignments-manager.component.html',
})
export class AssignmentsManagerComponent implements OnInit {
  private readonly api = inject(AssignmentApiService);
  private readonly supervisorApi = inject(SupervisorApiService);

  readonly assignments = signal<Assignment[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly pageSize = 25;
  readonly evaluators = signal<RoleUser[]>([]);
  readonly ideaOptions = signal<IdeaOption[]>([]);
  readonly filterEvaluatorId = signal<string>('');
  readonly filterStatus = signal<string>('');
  readonly filterIdeaSearch = signal<string>('');
  readonly selectedIds = signal<Set<string>>(new Set());
  readonly createDraft = signal<CreateDraft>({ ...EMPTY_DRAFT });
  readonly errorMessage = signal<string | null>(null);
  readonly loading = signal(true);
  readonly loadError = signal<string | null>(null);

  private initPromise: Promise<void> | null = null;

  // Angular invokes ngOnInit automatically on the first change-detection pass; tests in this
  // codebase also await it directly to synchronize async setup before assertions. Caching the
  // in-flight promise keeps a second invocation from re-fetching/re-reloading.
  ngOnInit(): Promise<void> {
    if (!this.initPromise) {
      this.initPromise = this.load();
    }
    return this.initPromise;
  }

  retryLoad(): Promise<void> {
    return this.load();
  }

  private async load(): Promise<void> {
    this.loading.set(true);
    this.loadError.set(null);
    try {
      this.evaluators.set(await this.supervisorApi.getUsersByRole('evaluator'));
      this.ideaOptions.set(await this.api.listIdeaOptions());
      await this.reload();
    } catch (error) {
      this.loadError.set(
        this.extractErrorMessage(error, $localize`:@@assignmentsLoadError:Couldn't load assignments. Please try again.`),
      );
    } finally {
      this.loading.set(false);
    }
  }

  private async reload(): Promise<void> {
    const result = await this.api.list({
      evaluatorId: this.filterEvaluatorId() || undefined,
      status: this.filterStatus() || undefined,
      ideaSearch: this.filterIdeaSearch() || undefined,
      page: this.page(),
      pageSize: this.pageSize,
    });
    this.assignments.set(result.items);
    this.total.set(result.total);
  }

  async applyFilters(): Promise<void> {
    this.page.set(1);
    await this.reload();
  }

  async applyExternalFilter(evaluatorId: string, status: string): Promise<void> {
    this.filterEvaluatorId.set(evaluatorId);
    this.filterStatus.set(status);
    this.page.set(1);
    await this.reload();
  }

  async goToPage(page: number): Promise<void> {
    this.page.set(page);
    await this.reload();
  }

  toggleSelected(id: string): void {
    this.selectedIds.update((set) => {
      const next = new Set(set);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  async onReassign(id: string, evaluatorId: string): Promise<void> {
    const assignment = this.assignments().find((a) => a.id === id);
    if (!assignment) return;
    this.errorMessage.set(null);
    try {
      await this.api.update(id, { statusCode: assignment.statusCode, dueAt: assignment.dueAt, notes: assignment.notes, evaluatorId });
      await this.reload();
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
    }
  }

  async onUnassign(id: string): Promise<void> {
    this.errorMessage.set(null);
    try {
      await this.api.unassign(id);
      await this.reload();
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
    }
  }

  async onBulkUnassign(): Promise<void> {
    const ids = Array.from(this.selectedIds());
    if (ids.length === 0) return;
    this.errorMessage.set(null);
    try {
      await this.api.bulkUnassign(ids);
      this.selectedIds.set(new Set());
      await this.reload();
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
    }
  }

  updateCreateDraft(patch: Partial<CreateDraft>): void {
    this.createDraft.update((d) => ({ ...d, ...patch }));
  }

  async onCreate(): Promise<void> {
    const draft = this.createDraft();
    if (!draft.ideaId || !draft.evaluatorId) {
      this.errorMessage.set($localize`An idea and an evaluator are required.`);
      return;
    }
    this.errorMessage.set(null);
    try {
      const input: AssignmentCreateInput = {
        ideaId: draft.ideaId,
        evaluatorId: draft.evaluatorId,
        dueAt: draft.dueAt || null,
        notes: draft.notes || null,
      };
      await this.api.create(input);
      this.createDraft.set({ ...EMPTY_DRAFT });
      await this.reload();
    } catch (error) {
      this.errorMessage.set(this.extractErrorMessage(error));
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
